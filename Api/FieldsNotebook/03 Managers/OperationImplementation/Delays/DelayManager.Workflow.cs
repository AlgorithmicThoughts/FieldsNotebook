using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Projects;
using DataContract.Delays;
using Engines.Delays;

namespace Managers.Delays;

/// <summary>
/// The delay workflow writes. Each action validates project access, role and crew authority, and the
/// delay's current state, then hands one atomic write to the workflow Accessor: the status move, the
/// cycle change, any handover check, the activity event, and the recipients' notices.
/// </summary>
public partial class DelayManager
{
    public async Task<DelayActionResponse> AcknowledgeAsync(
        int userId, int projectId, int delayId, AcknowledgeDelayRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Choice);
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);

        RequireAuthority(DelayWorkflowEngine.DirectsCrew(access, state.OwnerCrewId), delayId, "acknowledge");
        RequireOpenCycle(state);
        RequireState(state.StatusCode is DelayStatusCodes.Raised or DelayStatusCodes.Reopened, delayId,
            "only a raised or reopened delay can be acknowledged.");
        RequireState(!state.IsCycleAcknowledged, delayId, "this cycle is already acknowledged.");

        // A tentative completion time is optional here: the answer can be that the work is already ready.
        var expectedReadyAt = request.ExpectedReadyAt?.ToUniversalTime();
        var plan = new DelayActionPlan(DelayEventTypes.Acknowledged, DelayStatusCodes.Acknowledged,
            DelayCycleChange.Acknowledge(state.CycleId, new DelayAcknowledgmentChange(request.Choice, request.Note), expectedReadyAt),
            null, expectedReadyAt is null ? null : state.ExpectedReadyAt, expectedReadyAt, AffectedCrews(state));
        var acknowledgment = await CommitAsync(state, plan, userId, cancellationToken);

        if (!DelayAcknowledgmentChoices.IsReadyForHandover(request.Choice))
        {
            return acknowledgment;
        }

        // Nothing remains for the responsible crew, so the same answer asks the affected crews for their
        // handover checks. Each step keeps its own event, so the history still reads as two actions.
        var acknowledged = await Workflow.GetWorkflowStateAsync(state.ProjectId, state.DelayId, cancellationToken)
            ?? throw new DelayNotFoundException(state.DelayId);

        return await CommitAsync(acknowledged, HandoverCheckPlan(acknowledged), userId, cancellationToken);
    }

    public async Task<DelayActionResponse> ReviseCompletionTimeAsync(
        int userId, int projectId, int delayId, ReviseCompletionTimeRequest request, CancellationToken cancellationToken)
    {
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);

        RequireAuthority(DelayWorkflowEngine.DirectsCrew(access, state.OwnerCrewId), delayId, "revise the completion time of");
        RequireOpenCycle(state);
        RequireState(DelayWorkflowEngine.IsActive(state.StatusCode), delayId,
            "a closed delay has no tentative completion time to revise.");

        // The revision updates the active cycle and appends an event. It never opens another cycle.
        var expectedReadyAt = request.ExpectedReadyAt.ToUniversalTime();
        var plan = new DelayActionPlan(DelayEventTypes.ExpectedReadyChanged, null,
            DelayCycleChange.ReviseExpectedReady(state.CycleId, expectedReadyAt),
            null, state.ExpectedReadyAt, expectedReadyAt, AffectedCrews(state));

        return await CommitAsync(state, plan, userId, cancellationToken);
    }

    public async Task<DelayActionResponse> RequestHandoverCheckAsync(
        int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);

        RequireAuthority(DelayWorkflowEngine.DirectsCrew(access, state.OwnerCrewId), delayId, "request a handover check on");
        RequireOpenCycle(state);
        RequireState(state.StatusCode == DelayStatusCodes.Acknowledged, delayId,
            "a handover check follows an acknowledgment.");

        return await CommitAsync(state, HandoverCheckPlan(state), userId, cancellationToken);
    }

    public async Task<DelayActionResponse> VerifyHandoverAsync(
        int userId, int projectId, int delayId, VerifyHandoverRequest request, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.ImpactId, 1);
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);
        var impact = state.Impacts.FirstOrDefault(i => i.ImpactId == request.ImpactId);

        RequireState(impact is not null, delayId, "that affected crew is not on this delay.");
        // Only the affected crew's own Foreman can accept the handover for that crew.
        RequireAuthority(DelayWorkflowEngine.LeadsCrew(access, impact!.BlockedCrewId), delayId, "verify the handover of");
        RequireOpenCycle(state);
        RequireState(state.StatusCode == DelayStatusCodes.ReadyForHandover, delayId,
            "the responsible Superintendent has not asked for a handover check yet.");
        RequireState(!impact.IsVerifiedInCycle, delayId, "that crew already verified the handover in this cycle.");

        var plan = new DelayActionPlan(DelayEventTypes.HandoverVerified, null, DelayCycleChange.Touch(state.CycleId),
            new DelayVerificationChange(impact.ImpactId, request.Note), null, null, [state.OwnerCrewId]);

        return await CommitAsync(state, plan, userId, cancellationToken);
    }

    public async Task<DelayActionResponse> ResolveAsync(
        int userId, int projectId, int delayId, ResolveDelayRequest request, CancellationToken cancellationToken)
    {
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);

        RequireAuthority(DelayWorkflowEngine.DirectsCrew(access, state.OwnerCrewId), delayId, "resolve");
        RequireOpenCycle(state);
        RequireState(state.StatusCode == DelayStatusCodes.ReadyForHandover, delayId,
            "a delay resolves after its handover checks.");
        RequireState(DelayWorkflowEngine.HasEveryHandoverCheck(state), delayId,
            "every affected crew must verify the handover first.");
        // Every resolution, first or after a reopen, proves the work with its own photos.
        DelayPhotoRules.Validate(request.Photos, "Attach one to five proof-of-work photos to resolve the delay.");

        var plan = new DelayActionPlan(DelayEventTypes.Resolved, DelayStatusCodes.Resolved,
            DelayCycleChange.CloseCycle(state.CycleId), null, null, null, EveryCrew(state),
            DelayPhotoRules.ToUploads(request.Photos));

        return await CommitAsync(state, plan, userId, cancellationToken);
    }

    public async Task<DelayActionResponse> ReopenAsync(
        int userId, int projectId, int delayId, ReopenDelayRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reason);
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);

        RequireAuthority(DelayWorkflowEngine.IsSuperintendent(access), delayId, "reopen");
        RequireState(state.StatusCode == DelayStatusCodes.Resolved, delayId, "only a resolved delay can be reopened.");
        RequireState(!state.IsCycleOpen, delayId, "this delay still has an open cycle.");

        // A reopen opens a cycle, which keeps the earlier acknowledgment and handover checks on their own cycle.
        var plan = new DelayActionPlan(DelayEventTypes.Reopened, DelayStatusCodes.Reopened,
            DelayCycleChange.Open(request.Reason), null, null, null, EveryCrew(state));

        return await CommitAsync(state, plan, userId, cancellationToken);
    }

    public async Task<DelayActionResponse> RevokeAsync(
        int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var (access, state) = await LoadAsync(userId, projectId, delayId, cancellationToken);

        // The Superintendent of the Foreman who raised the delay withdraws it, not the responsible one.
        RequireAuthority(DelayWorkflowEngine.DirectsCrew(access, state.ReportedByCrewId), delayId, "withdraw");
        RequireOpenCycle(state);
        RequireState(DelayWorkflowEngine.IsActive(state.StatusCode), delayId, "this delay is already closed.");

        var plan = new DelayActionPlan(DelayEventTypes.Revoked, DelayStatusCodes.Revoked,
            DelayCycleChange.CloseCycle(state.CycleId), null, null, null, EveryCrew(state));

        return await CommitAsync(state, plan, userId, cancellationToken);
    }

    /// <summary>One validated action, ready to become a workflow write.</summary>
    /// <param name="ToStatusCode">Null when the action changes no status, such as a revised completion time.</param>
    /// <param name="RecipientCrewIds">The crews whose Foremen and Superintendents this action concerns.</param>
    /// <param name="ProofOfWorkPhotos">Only a resolution carries proof-of-work photos.</param>
    private sealed record DelayActionPlan(
        string EventType,
        string? ToStatusCode,
        DelayCycleChange Cycle,
        DelayVerificationChange? Verification,
        DateTimeOffset? OldExpectedReadyAt,
        DateTimeOffset? NewExpectedReadyAt,
        IReadOnlyList<int> RecipientCrewIds,
        IReadOnlyList<ReportPhotoUploadRecord>? ProofOfWorkPhotos = null);

    private IDelayWorkflowAccessor Workflow => accessorFactory.CreateAccessor<IDelayWorkflowAccessor>();

    private async Task<(ProjectAccessRecord Access, DelayWorkflowState State)> LoadAsync(
        int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(delayId, 1);

        var access = await accessorFactory.CreateAccessor<IProjectAccessAccessor>()
            .GetProjectAccessAsync(userId, projectId, cancellationToken)
            ?? throw new ProjectAccessDeniedException(projectId);
        var state = await Workflow.GetWorkflowStateAsync(projectId, delayId, cancellationToken)
            ?? throw new DelayNotFoundException(delayId);

        return (access, state);
    }

    private async Task<DelayActionResponse> CommitAsync(
        DelayWorkflowState state, DelayActionPlan plan, int actorUserId, CancellationToken cancellationToken)
    {
        var workflow = Workflow;
        int? toStatusId = null;

        if (plan.ToStatusCode is { } statusCode)
        {
            toStatusId = await workflow.GetStatusIdAsync(statusCode, cancellationToken)
                ?? throw new DelayStateConflictException(state.DelayId, $"status {statusCode} is not available.");
        }

        var recipients = await RecipientsAsync(state.ProjectId, plan.RecipientCrewIds, actorUserId, cancellationToken);
        var occurredAt = DateTimeOffset.UtcNow;
        var result = await workflow.ApplyAsync(new DelayWorkflowWrite(state.DelayId, state.ProjectId, actorUserId,
            occurredAt, plan.EventType, state.StatusId, toStatusId, plan.Cycle, plan.Verification,
            plan.OldExpectedReadyAt, plan.NewExpectedReadyAt, recipients, plan.ProofOfWorkPhotos ?? []), cancellationToken);

        return new DelayActionResponse(state.DelayId, plan.ToStatusCode ?? state.StatusCode, result.CycleId,
            result.ActivityEventId, plan.EventType, occurredAt, result.NoticeCount);
    }

    /// <summary>The people this action notifies, found from crew relationships and never the actor.</summary>
    private async Task<IReadOnlyList<int>> RecipientsAsync(
        int projectId, IReadOnlyList<int> crewIds, int actorUserId, CancellationToken cancellationToken)
    {
        if (crewIds.Count == 0)
        {
            return [];
        }

        var contacts = await Workflow.GetCrewContactsAsync(projectId, crewIds, cancellationToken);

        return contacts.Where(id => id != actorUserId).ToList();
    }

    /// <summary>The move to ReadyForHandover, asked for on its own or by an acknowledgment that needs no work.</summary>
    private static DelayActionPlan HandoverCheckPlan(DelayWorkflowState state) =>
        new(DelayEventTypes.ReadyForHandover, DelayStatusCodes.ReadyForHandover,
            DelayCycleChange.Touch(state.CycleId), null, null, null, AffectedCrews(state));

    /// <summary>Every affected crew of the delay. The list is never reduced to one.</summary>
    private static IReadOnlyList<int> AffectedCrews(DelayWorkflowState state) =>
        state.Impacts.Select(i => i.BlockedCrewId).Distinct().ToList();

    private static IReadOnlyList<int> EveryCrew(DelayWorkflowState state) =>
        AffectedCrews(state).Append(state.OwnerCrewId).Distinct().ToList();

    private static void RequireAuthority(bool permitted, int delayId, string action)
    {
        if (!permitted)
        {
            throw new DelayActionNotPermittedException(delayId, action);
        }
    }

    private static void RequireState(bool satisfied, int delayId, string reason)
    {
        if (!satisfied)
        {
            throw new DelayStateConflictException(delayId, reason);
        }
    }

    private static void RequireOpenCycle(DelayWorkflowState state) =>
        RequireState(state.IsCycleOpen, state.DelayId, "this delay has no open cycle.");
}
