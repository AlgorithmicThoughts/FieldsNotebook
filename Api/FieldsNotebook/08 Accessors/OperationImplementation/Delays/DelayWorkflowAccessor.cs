using Common.Delays;
using DataContract.Domain.Delays;
using Common.Errors;
using Common.Results;
using Database;
using Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Delays;

public class DelayWorkflowAccessor(FieldNotebookContext context) : IDelayWorkflowAccessor
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
    /// A refused step rolls the whole action back and returns a conflict; only failures nobody expects throw.
    /// </summary>
    public async Task<Result<DelayWorkflowResult>> ApplyAsync(IReadOnlyList<DelayWorkflowWrite> writes,
        IReadOnlyList<ReportPhotoRecord> proofOfWorkPhotos, CancellationToken cancellationToken)
    {
        if (writes.Count == 0)
        {
            throw new ArgumentException("A workflow action applies at least one step.", nameof(writes));
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        DelayWorkflowResult? result = null;
        foreach (var write in writes)
        {
            var photos = write.ProofOfWorkPhotos.Count > 0 ? proofOfWorkPhotos : [];
            result = await ApplyStepAsync(write, photos, cancellationToken);
            if (result is null)
            {
                // Leaving without a commit rolls the transaction back. Clearing the tracker keeps any
                // pending entity changes from reaching a later save in the same request scope.
                context.ChangeTracker.Clear();
                return DelayErrors.StateConflict(write.DelayId, "another action changed its status first.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result!;
    }

    #region Private Methods

    /// <returns>Null when the delay is no longer in the status the step names.</returns>
    private async Task<DelayWorkflowResult?> ApplyStepAsync(
        DelayWorkflowWrite write, IReadOnlyList<ReportPhotoRecord> proofOfWorkPhotos, CancellationToken cancellationToken)
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
        AddProofOfWork(write, cycleId, proofOfWorkPhotos);

        var activityEvent = AddEvent(write, cycleId, fromStatusId, toStatusId);
        await context.SaveChangesAsync(cancellationToken);

        var noticeCount = AddNotices(write, activityEvent.Id);
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

    private void AddProofOfWork(DelayWorkflowWrite write, int cycleId, IReadOnlyList<ReportPhotoRecord> uploaded)
    {
        // CreatedAt matches the submission time, so each photo precedes the cycle's ClosedAt.
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
    // verification or revision that races a closing acceptance or revoke onto a closed cycle.
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
            DelayImpactId = write.Verification?.ImpactId ?? write.Return?.ImpactId,
            ActorUserId = write.ActorUserId,
            EventType = write.EventType,
            FromDelayStatusId = toStatusId is null ? null : fromStatusId,
            ToDelayStatusId = toStatusId,
            OldExpectedReadyAt = write.OldExpectedReadyAt,
            NewExpectedReadyAt = write.NewExpectedReadyAt,
            ReturnReason = write.Return?.Reason,
            CreatedAt = write.OccurredAt,
        };

        context.ActivityEvents.Add(activityEvent);
        return activityEvent;
    }

    /// <summary>One notice per recipient the workflow Engine named for the step.</summary>
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

    #endregion
}
