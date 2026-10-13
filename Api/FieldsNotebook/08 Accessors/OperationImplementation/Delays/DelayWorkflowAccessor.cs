using Common.Delays;
using DataContract.Domain.Delays;
using Common.Errors;
using Common.Results;
using Database;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utilities;
using Utilities.OperationContract;

namespace Accessors.Delays
{
    public class DelayWorkflowAccessor(FieldNotebookContext context, UtilityFactory utilities,
        ILogger<DelayWorkflowAccessor> logger) : IDelayWorkflowAccessor
    {
        public async Task<DelayWorkflowState?> GetWorkflowStateAsync(
            int projectId, int delayId, CancellationToken cancellationToken)
        {
            var delay = await (
                from d in context.Delays.AsNoTracking()
                join status in context.DelayStatus.AsNoTracking() on d.DelayStatusId equals status.Id
                where d.Id == delayId && d.ProjectId == projectId
                select new { status.Code, d.DelayStatusId, d.OwnerCrewId, d.ReportedByCrewId })
                .SingleOrDefaultAsync(cancellationToken);
            // The latest cycle is the one every action reads: open for an active delay, closed otherwise.
            var cycle = await context.DelayCycles.AsNoTracking()
                .Where(c => c.DelayId == delayId)
                .OrderByDescending(c => c.DelayCycleId)
                .Select(c => new { c.DelayCycleId, c.ClosedAt, c.AcknowledgedAt, c.ExpectedReadyAt })
                .FirstOrDefaultAsync(cancellationToken);
            if (delay is null || cycle is null)
            {
                return null;
            }

            var cycleCount = await context.DelayCycles.AsNoTracking()
                .CountAsync(c => c.DelayId == delayId, cancellationToken);
            var impacts = await (
                from i in context.DelayImpacts.AsNoTracking()
                where i.DelayId == delayId
                orderby i.Id
                select new DelayWorkflowImpactRecord(i.Id, i.BlockedCrewId,
                    context.DelayImpactVerifications.AsNoTracking()
                        .Any(v => v.DelayImpactId == i.Id && v.DelayCycleId == cycle.DelayCycleId)))
                .ToListAsync(cancellationToken);

            return new DelayWorkflowState(delayId, projectId, delay.Code, delay.DelayStatusId, delay.OwnerCrewId,
                delay.ReportedByCrewId, cycle.DelayCycleId, cycle.ClosedAt is null, cycle.AcknowledgedAt is not null,
                cycleCount, cycle.ExpectedReadyAt, impacts);
        }

        /// <summary>
        /// One transaction covers every step. Each activity event saves before its notices because the
        /// generated event ID is their foreign key and the mapping carries no navigation to resolve it.
        /// Proof-of-work photos upload after the conditional status move, so a concurrent action
        /// refuses the write before any file is stored, and a later failure removes what was uploaded.
        /// A refused step rolls the whole action back and returns a conflict; only failures nobody expects throw.
        /// </summary>
        public async Task<Result<DelayWorkflowResult>> ApplyAsync(IReadOnlyList<DelayWorkflowWrite> writes, CancellationToken cancellationToken)
        {
            if (writes.Count == 0)
            {
                throw new ArgumentException("A workflow action applies at least one step.", nameof(writes));
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var photoStorage = writes.Any(w => w.ProofOfWorkPhotos.Count > 0)
                ? new DelayPhotoStorage(utilities.CreateUtility<IAzureBlobStorageUtility>(), logger)
                : null;
            try
            {
                DelayWorkflowResult? result = null;
                foreach (var write in writes)
                {
                    result = await ApplyStepAsync(write, photoStorage, cancellationToken);
                    if (result is null)
                    {
                        // Leaving without a commit rolls the transaction back. Clearing the tracker keeps any
                        // pending entity changes from reaching a later save in the same request scope.
                        context.ChangeTracker.Clear();
                        await CleanUpAsync(photoStorage);
                        return DelayErrors.StateConflict(write.DelayId, "another action changed its status first.");
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return result!;
            }
            catch
            {
                await CleanUpAsync(photoStorage);
                throw;
            }
        }

        #region Private Methods

        private static async Task CleanUpAsync(DelayPhotoStorage? photoStorage)
        {
            if (photoStorage is not null)
            {
                await photoStorage.CleanUpAsync();
            }
        }

        /// <returns>Null when the delay is no longer in the status the step names.</returns>
        private async Task<DelayWorkflowResult?> ApplyStepAsync(
            DelayWorkflowWrite write, DelayPhotoStorage? photoStorage, CancellationToken cancellationToken)
        {
            // The current status may since have been retired, so only the target status must be active.
            var fromStatusId = await GetStatusIdAsync(write.FromStatusCode, activeOnly: false, cancellationToken);
            int? toStatusId = write.ToStatusCode is { } toStatusCode
                ? await GetStatusIdAsync(toStatusCode, activeOnly: true, cancellationToken)
                : null;

            var cycleId = await SaveCycleAsync(write, cancellationToken);
            if (await MoveStatusAsync(write, fromStatusId, toStatusId, cancellationToken) is false)
            {
                return null;
            }

            AddVerification(write, cycleId);
            if (write.ProofOfWorkPhotos.Count > 0)
            {
                await AddProofOfWorkAsync(write, cycleId, photoStorage!, cancellationToken);
            }

            var activityEvent = AddEvent(write, cycleId, fromStatusId, toStatusId);
            await context.SaveChangesAsync(cancellationToken);

            var noticeCount = await AddNoticesAsync(write, activityEvent.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new DelayWorkflowResult(cycleId, activityEvent.Id, noticeCount);
        }

        private async Task<int> GetStatusIdAsync(string statusCode, bool activeOnly, CancellationToken cancellationToken)
        {
            var statuses = context.DelayStatus.AsNoTracking().Where(s => s.Code == statusCode);
            if (activeOnly)
            {
                statuses = statuses.Where(s => s.IsActive);
            }

            return await statuses.Select(s => (int?)s.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Required delay status {statusCode} is not configured.");
        }

        /// <summary>The active Foremen and Superintendents of the crews who hold access to the project.</summary>
        private async Task<IReadOnlyList<int>> GetCrewContactsAsync(
            int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken)
        {
            if (crewIds.Count == 0)
            {
                return [];
            }

            var crews = context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive && crewIds.Contains(c.Id));
            var contacts = crews.Select(c => c.ForemanLeadId).Union(crews.Select(c => c.SuperintendentUserId));

            return await (
                from up in context.UserProjects.AsNoTracking()
                join u in context.Users.AsNoTracking() on up.UserId equals u.Id
                where up.ProjectId == projectId && up.IsActive && u.IsActive && contacts.Contains(up.UserId)
                orderby up.UserId
                select up.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        private async Task AddProofOfWorkAsync(
            DelayWorkflowWrite write, int cycleId, DelayPhotoStorage photoStorage, CancellationToken cancellationToken)
        {
            var prefix = $"project-{write.ProjectId}/{write.OccurredAt:yyyy-MM-dd}/report-{write.DelayId}/proof-of-work/cycle-{cycleId}";
            var uploaded = await photoStorage.UploadAsync(write.ProofOfWorkPhotos, prefix, cancellationToken);

            // CreatedAt matches the closure time, so each photo precedes or equals the cycle's ClosedAt.
            context.DelayPhotos.AddRange(uploaded.Select(p => new DelayPhoto
            {
                DelayId = write.DelayId,
                DelayCycleId = cycleId,
                Purpose = DelayPhotoPurposes.ProofOfWork,
                CreatedByUserId = write.ActorUserId,
                OriginalUrl = p.OriginalUrl,
                ThumbnailUrl = p.ThumbnailUrl,
                FileName = p.FileName,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                CreatedAt = write.OccurredAt,
            }));
        }

        private async Task<int> SaveCycleAsync(DelayWorkflowWrite write, CancellationToken cancellationToken)
        {
            if (write.Cycle.CycleId is { } cycleId)
            {
                ApplyCycleChange(await context.DelayCycles.FirstAsync(c => c.DelayCycleId == cycleId, cancellationToken), write);
                return cycleId;
            }

            // A reopen opens a cycle. The filtered unique index on DelayCycles keeps a delay to one open cycle.
            var opened = new DelayCycle
            {
                DelayId = write.DelayId,
                CreatedAt = write.OccurredAt,
                CreatedBy = write.ActorUserId,
                OpenReason = write.Cycle.OpenReason,
            };

            context.DelayCycles.Add(opened);
            await context.SaveChangesAsync(cancellationToken);
            return opened.DelayCycleId;
        }

        private static void ApplyCycleChange(DelayCycle cycle, DelayWorkflowWrite write)
        {
            cycle.ModifiedAt = write.OccurredAt;
            cycle.ModifiedBy = write.ActorUserId;

            if (write.Cycle.Acknowledgment is { } acknowledgment)
            {
                cycle.AcknowledgmentChoice = acknowledgment.Choice;
                cycle.AcknowledgmentNote = acknowledgment.Note;
                cycle.AcknowledgedByUserId = write.ActorUserId;
                cycle.AcknowledgedAt = write.OccurredAt;
            }

            if (write.Cycle.ExpectedReadyAt is { } expectedReadyAt)
            {
                cycle.ExpectedReadyAt = expectedReadyAt;
            }

            if (write.Cycle.Close)
            {
                cycle.ClosedByUserId = write.ActorUserId;
                cycle.ClosedAt = write.OccurredAt;
            }
        }

        // Conditional on the status the Engine validated, so a concurrent action cannot be silently overwritten.
        // A step that keeps its status still runs the update: it locks the row until commit and refuses a
        // verification or revision that races a resolve or revoke onto a closed cycle.
        private async Task<bool> MoveStatusAsync(DelayWorkflowWrite write, int fromStatusId, int? toStatusId, CancellationToken cancellationToken)
        {
            var moved = await context.Delays
                .Where(d => d.Id == write.DelayId && d.DelayStatusId == fromStatusId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.DelayStatusId, toStatusId ?? fromStatusId), cancellationToken);
            return moved > 0;
        }

        private void AddVerification(DelayWorkflowWrite write, int cycleId)
        {
            if (write.Verification is not { } verification)
            {
                return;
            }

            context.DelayImpactVerifications.Add(new DelayImpactVerification
            {
                DelayImpactId = verification.ImpactId,
                DelayId = write.DelayId,
                DelayCycleId = cycleId,
                VerifiedByUserId = write.ActorUserId,
                VerifiedAt = write.OccurredAt,
                VerificationNote = verification.Note,
            });
        }

        private ActivityEvent AddEvent(DelayWorkflowWrite write, int cycleId, int fromStatusId, int? toStatusId)
        {
            var activityEvent = new ActivityEvent
            {
                ProjectId = write.ProjectId,
                DelayId = write.DelayId,
                DelayCycleId = cycleId,
                DelayImpactId = write.Verification?.ImpactId,
                ActorUserId = write.ActorUserId,
                EventType = write.EventType,
                FromDelayStatusId = toStatusId is null ? null : fromStatusId,
                ToDelayStatusId = toStatusId,
                OldExpectedReadyAt = write.OldExpectedReadyAt,
                NewExpectedReadyAt = write.NewExpectedReadyAt,
                CreatedAt = write.OccurredAt,
            };

            context.ActivityEvents.Add(activityEvent);
            return activityEvent;
        }

        private async Task<int> AddNoticesAsync(DelayWorkflowWrite write, int activityEventId, CancellationToken cancellationToken)
        {
            // Read inside the transaction, so the notices match the crews as they stood when the step applied.
            // The actor never receives a notice of their own action.
            var contacts = await GetCrewContactsAsync(write.ProjectId, write.RecipientCrewIds, cancellationToken);
            var recipients = contacts.Where(id => id != write.ActorUserId).ToList();
            foreach (var userId in recipients)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    ProjectId = write.ProjectId,
                    DelayId = write.DelayId,
                    ActivityEventId = activityEventId,
                    CreatedAt = write.OccurredAt,
                });
            }

            return recipients.Count;
        }

        #endregion
    }
}
