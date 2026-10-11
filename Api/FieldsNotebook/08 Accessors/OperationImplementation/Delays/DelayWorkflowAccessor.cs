using Common.Delays;
using Common.Errors;
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

        public async Task<int?> GetStatusIdAsync(string statusCode, CancellationToken cancellationToken) =>
            await context.DelayStatus.AsNoTracking()
                .Where(s => s.Code == statusCode && s.IsActive)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyList<int>> GetCrewContactsAsync(
            int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken)
        {
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

        /// <summary>
        /// One transaction covers all of it. The activity event saves before its notices because the
        /// generated event ID is their foreign key and the mapping carries no navigation to resolve it.
        /// Proof-of-work photos upload after the conditional status move, so a concurrent action
        /// refuses the write before any file is stored, and a later failure removes what was uploaded.
        /// </summary>
        public async Task<DelayWorkflowResult> ApplyAsync(DelayWorkflowWrite write, CancellationToken cancellationToken)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var photoStorage = write.ProofOfWorkPhotos.Count == 0
                ? null
                : new DelayPhotoStorage(utilities.CreateManager<IAzureBlobStorageUtility>(), logger);
            try
            {
                var cycleId = await SaveCycleAsync(write, cancellationToken);
                await MoveStatusAsync(write, cancellationToken);
                AddVerification(write, cycleId);
                if (photoStorage is not null)
                {
                    await AddProofOfWorkAsync(write, cycleId, photoStorage, cancellationToken);
                }

                var activityEvent = AddEvent(write, cycleId);
                await context.SaveChangesAsync(cancellationToken);

                var noticeCount = AddNotices(write, activityEvent.Id);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new DelayWorkflowResult(cycleId, activityEvent.Id, noticeCount);
            }
            catch
            {
                if (photoStorage is not null)
                {
                    await photoStorage.CleanUpAsync();
                }

                throw;
            }
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

        // Conditional on the status the Manager validated, so a concurrent action cannot be silently overwritten.
        private async Task MoveStatusAsync(DelayWorkflowWrite write, CancellationToken cancellationToken)
        {
            if (write.ToStatusId is not { } toStatusId)
            {
                return;
            }

            var moved = await context.Delays
                .Where(d => d.Id == write.DelayId && d.DelayStatusId == write.FromStatusId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.DelayStatusId, toStatusId), cancellationToken);
            if (moved == 0)
            {
                throw new DelayStateConflictException(write.DelayId, "another action changed its status first.");
            }
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

        private ActivityEvent AddEvent(DelayWorkflowWrite write, int cycleId)
        {
            var activityEvent = new ActivityEvent
            {
                ProjectId = write.ProjectId,
                DelayId = write.DelayId,
                DelayCycleId = cycleId,
                DelayImpactId = write.Verification?.ImpactId,
                ActorUserId = write.ActorUserId,
                EventType = write.EventType,
                FromDelayStatusId = write.ToStatusId is null ? null : write.FromStatusId,
                ToDelayStatusId = write.ToStatusId,
                OldExpectedReadyAt = write.OldExpectedReadyAt,
                NewExpectedReadyAt = write.NewExpectedReadyAt,
                CreatedAt = write.OccurredAt,
            };

            context.ActivityEvents.Add(activityEvent);
            return activityEvent;
        }

        private int AddNotices(DelayWorkflowWrite write, int activityEventId)
        {
            foreach (var userId in write.RecipientUserIds)
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

            return write.RecipientUserIds.Count;
        }
    }
}
