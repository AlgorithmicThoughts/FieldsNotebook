using Common.Delays;
using Common.Errors;
using Common.Results;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;

namespace Engines.Delays;

/// <summary>
/// Who may move a delay and when, from the approved workflow. Each action checks its row of the
/// transition table, then returns either the writes that carry it out or the reason it is refused.
/// Nothing here reaches the database or the HTTP contract.
/// </summary>
public sealed partial class DelayWorkflowEngine : IDelayWorkflowEngine
{
    private static readonly string[] ActiveStatusCodes =
        [DelayStatusCodes.Raised, DelayStatusCodes.Acknowledged, DelayStatusCodes.ReadyForHandover, DelayStatusCodes.Reopened];

    // The transition table: who may act, the statuses an action starts from, and the cycle it needs.
    private static readonly Transition AcknowledgeTransition = new("acknowledge",
        (c, _) => DirectsCrew(c.Access, c.State.OwnerCrewId),
        [DelayStatusCodes.Raised, DelayStatusCodes.Reopened], CycleOpen: true,
        "only a raised or reopened delay can be acknowledged.");

    private static readonly Transition ReviseCompletionTimeTransition = new("revise the completion time of",
        (c, _) => DirectsCrew(c.Access, c.State.OwnerCrewId),
        ActiveStatusCodes, CycleOpen: true,
        "a closed delay has no tentative completion time to revise.");

    // The responsible crew's Superintendent or its Foreman submits the finished work for review.
    private static readonly Transition RequestHandoverCheckTransition = new("submit for review",
        (c, _) => DirectsCrew(c.Access, c.State.OwnerCrewId) || LeadsCrew(c.Access, c.State.OwnerCrewId),
        [DelayStatusCodes.Acknowledged], CycleOpen: true,
        "a delay is submitted for review after its acknowledgment.");

    // Only the affected crew's own Foreman accepts the proof of work, and only for that crew.
    private static readonly Transition VerifyHandoverTransition = new("accept the proof of work on",
        (c, impact) => LeadsCrew(c.Access, impact!.BlockedCrewId),
        [DelayStatusCodes.ReadyForHandover], CycleOpen: true,
        "the responsible crew has not submitted this delay for review yet.");

    // The same Foreman who would accept may instead send the proof of work back for their crew.
    private static readonly Transition ReturnHandoverTransition = new("send back",
        (c, impact) => LeadsCrew(c.Access, impact!.BlockedCrewId),
        [DelayStatusCodes.ReadyForHandover], CycleOpen: true,
        "only a delay under review can be sent back.");

    private static readonly Transition ReopenTransition = new("reopen",
        (c, _) => IsSuperintendent(c.Access),
        [DelayStatusCodes.Resolved], CycleOpen: false,
        "only a resolved delay can be reopened.");

    // The Superintendent of the Foreman who raised the delay withdraws it, not the responsible one.
    private static readonly Transition RevokeTransition = new("withdraw",
        (c, _) => DirectsCrew(c.Access, c.State.ReportedByCrewId),
        ActiveStatusCodes, CycleOpen: true,
        "this delay is already closed.");

    public Result<IReadOnlyList<DelayWorkflowWrite>> Acknowledge(DelayWorkflowContext context, DelayAcknowledgmentChange acknowledgment,
        DateTimeOffset? expectedReadyAt)
    {
        var state = context.State;
        if (Refusal(AcknowledgeTransition, context) is { } refused)
        {
            return refused;
        }

        if (state.IsCycleAcknowledged)
        {
            return DelayErrors.StateConflict(state.DelayId, "this cycle is already acknowledged.");
        }

        // A tentative completion time is optional here: the answer can be that the work is already ready.
        // Even then the acknowledgment only records the answer; review needs its own submission with proof of work.
        var readyAt = expectedReadyAt?.ToUniversalTime();
        return Steps(Step(context, DelayEventTypes.Acknowledged, DelayStatusCodes.Acknowledged,
            DelayCycleChange.Acknowledge(state.CycleId, acknowledgment, readyAt), AffectedCrews(state)) with
        {
            OldExpectedReadyAt = readyAt is null ? null : state.ExpectedReadyAt,
            NewExpectedReadyAt = readyAt,
        });
    }

    public Result<IReadOnlyList<DelayWorkflowWrite>> ReviseCompletionTime(DelayWorkflowContext context, DateTimeOffset? expectedReadyAt)
    {
        var state = context.State;
        if (Refusal(ReviseCompletionTimeTransition, context) is { } refused)
        {
            return refused;
        }

        if (expectedReadyAt is not { } revised)
        {
            return RequestError.InvalidRequest("Enter the revised completion time.");
        }

        var readyAt = revised.ToUniversalTime();

        // The revision updates the active cycle and appends an event. It never opens another cycle.
        return Steps(Step(context, DelayEventTypes.ExpectedReadyChanged, null,
            DelayCycleChange.ReviseExpectedReady(state.CycleId, readyAt), AffectedCrews(state)) with
        {
            OldExpectedReadyAt = state.ExpectedReadyAt,
            NewExpectedReadyAt = readyAt,
        });
    }

    public Result<IReadOnlyList<DelayWorkflowWrite>> RequestHandoverCheck(DelayWorkflowContext context,
        IReadOnlyList<ReportPhotoUploadRecord> proofOfWork)
    {
        var state = context.State;
        if (Refusal(RequestHandoverCheckTransition, context) is { } refused)
        {
            return refused;
        }

        // Every submission, first or after a send-back, proves the work with its own photos.
        if (PhotoRules.Validate(proofOfWork, "Attach one to five proof-of-work photos to submit the delay for review.") is { } invalidPhoto)
        {
            return invalidPhoto;
        }

        // A crew that already accepted in this cycle stays unblocked, so only the crews still waiting review it.
        return Steps(Step(context, DelayEventTypes.ReadyForHandover, DelayStatusCodes.ReadyForHandover,
            DelayCycleChange.Touch(state.CycleId), PendingCrews(state)) with
        {
            ProofOfWorkPhotos = proofOfWork,
        });
    }

    public Result<IReadOnlyList<DelayWorkflowWrite>> VerifyHandover(DelayWorkflowContext context, DelayVerificationChange verification)
    {
        var state = context.State;
        var impact = state.Impacts.FirstOrDefault(i => i.ImpactId == verification.ImpactId);
        if (impact is null)
        {
            return DelayErrors.ImpactNotFound(state.DelayId, verification.ImpactId);
        }

        if (Refusal(VerifyHandoverTransition, context, impact) is { } refused)
        {
            return refused;
        }

        if (impact.IsVerifiedInCycle)
        {
            return DelayErrors.StateConflict(state.DelayId, "that crew already accepted the proof of work in this cycle.");
        }

        var isLastCrew = state.Impacts.All(i => i.ImpactId == impact.ImpactId || i.IsVerifiedInCycle);

        // The acceptance unblocks this crew only. The responsible crew hears about it unless it also closes the delay,
        // in which case the closing step tells every crew once instead of twice.
        var accepted = Step(context, DelayEventTypes.HandoverVerified, null, DelayCycleChange.Touch(state.CycleId),
            isLastCrew ? [] : [state.OwnerCrewId]) with
        {
            Verification = verification,
        };

        if (isLastCrew is false)
        {
            return Steps(accepted);
        }

        // The last waiting crew's acceptance closes the delay; nobody resolves it separately.
        return Steps(accepted, Step(context, DelayEventTypes.Resolved, DelayStatusCodes.Resolved,
            DelayCycleChange.CloseCycle(state.CycleId), EveryCrew(state)));
    }

    public Result<IReadOnlyList<DelayWorkflowWrite>> ReturnHandover(DelayWorkflowContext context, DelayReturnChange handoverReturn)
    {
        var state = context.State;
        var impact = state.Impacts.FirstOrDefault(i => i.ImpactId == handoverReturn.ImpactId);
        if (impact is null)
        {
            return DelayErrors.ImpactNotFound(state.DelayId, handoverReturn.ImpactId);
        }

        if (Refusal(ReturnHandoverTransition, context, impact) is { } refused)
        {
            return refused;
        }

        if (impact.IsVerifiedInCycle)
        {
            return DelayErrors.StateConflict(state.DelayId, "that crew already accepted the proof of work in this cycle.");
        }

        if (string.IsNullOrWhiteSpace(handoverReturn.Reason))
        {
            return RequestError.InvalidRequest("Say what the responsible crew must fix.");
        }

        // Back to Acknowledged, so the responsible crew fixes the work and submits fresh proof. Crews that already
        // accepted keep their acceptance, because it is recorded against their own impact.
        return Steps(Step(context, DelayEventTypes.HandoverReturned, DelayStatusCodes.Acknowledged,
            DelayCycleChange.Touch(state.CycleId), [state.OwnerCrewId]) with
        {
            Return = handoverReturn,
        });
    }

    public Result<IReadOnlyList<DelayWorkflowWrite>> Reopen(DelayWorkflowContext context, string reason)
    {
        if (Refusal(ReopenTransition, context) is { } refused)
        {
            return refused;
        }

        // A reopen opens a cycle, which keeps the earlier acknowledgment and handover checks on their own cycle.
        return Steps(Step(context, DelayEventTypes.Reopened, DelayStatusCodes.Reopened, DelayCycleChange.Open(reason), EveryCrew(context.State)));
    }

    public Result<IReadOnlyList<DelayWorkflowWrite>> Revoke(DelayWorkflowContext context)
    {
        var state = context.State;
        if (Refusal(RevokeTransition, context) is { } refused)
        {
            return refused;
        }

        return Steps(Step(context, DelayEventTypes.Revoked, DelayStatusCodes.Revoked, DelayCycleChange.CloseCycle(state.CycleId), EveryCrew(state)));
    }

    #region Private Methods

    /// <summary>The user is the Superintendent who directs this crew.</summary>
    private static bool DirectsCrew(ProjectAccessRecord access, int crewId)
    {
        return access.RoleName == RoleNames.Superintendent
            && access.Crews.Any(c => c.CrewId == crewId && c.IsForemanLead is false);
    }

    /// <summary>The user is the Foreman who leads this crew.</summary>
    private static bool LeadsCrew(ProjectAccessRecord access, int crewId)
    {
        return access.RoleName == RoleNames.Foreman
            && access.Crews.Any(c => c.CrewId == crewId && c.IsForemanLead);
    }

    /// <summary>Any Superintendent with access to the project reopens a resolved delay.</summary>
    private static bool IsSuperintendent(ProjectAccessRecord access)
    {
        return access.RoleName == RoleNames.Superintendent;
    }

    /// <summary>
    /// Why the transition does not apply, checked in a fixed order: authority, then the cycle, then the
    /// status. Null when the action may proceed.
    /// </summary>
    private static RequestError? Refusal(Transition transition, DelayWorkflowContext context, DelayWorkflowImpactRecord? impact = null)
    {
        var state = context.State;
        if (transition.IsPermitted(context, impact) is false)
        {
            return DelayErrors.NotPermitted(state.DelayId, transition.Verb);
        }

        if (state.IsCycleOpen != transition.CycleOpen)
        {
            return DelayErrors.StateConflict(state.DelayId,
                transition.CycleOpen ? "this delay has no open cycle." : "this delay still has an open cycle.");
        }

        if (transition.FromStatusCodes.Contains(state.StatusCode) is false)
        {
            return DelayErrors.StateConflict(state.DelayId, transition.WrongStatusReason);
        }

        return null;
    }

    // The writes are an interface type, which C# will not convert implicitly, so success is spelled out once here.
    private static Result<IReadOnlyList<DelayWorkflowWrite>> Steps(params DelayWorkflowWrite[] writes) =>
        Result<IReadOnlyList<DelayWorkflowWrite>>.Success(writes);

    private static DelayWorkflowWrite Step(DelayWorkflowContext context, string eventType, string? toStatusCode,
        DelayCycleChange cycle, IReadOnlyList<int> recipientCrewIds) =>
        new(context.State.DelayId, context.State.ProjectId, context.ActorUserId, context.OccurredAt, eventType,
            context.State.StatusCode, toStatusCode, cycle, NotifyCrews(context, recipientCrewIds));

    /// <summary>Every affected crew of the delay. The list is never reduced to one.</summary>
    private static IReadOnlyList<int> AffectedCrews(DelayWorkflowState state) =>
        state.Impacts.Select(i => i.BlockedCrewId).Distinct().ToList();

    /// <summary>The affected crews that have not accepted the proof of work in the current cycle.</summary>
    private static IReadOnlyList<int> PendingCrews(DelayWorkflowState state) =>
        state.Impacts.Where(i => i.IsVerifiedInCycle is false).Select(i => i.BlockedCrewId).Distinct().ToList();

    private static IReadOnlyList<int> EveryCrew(DelayWorkflowState state) =>
        AffectedCrews(state).Append(state.OwnerCrewId).Distinct().ToList();

    /// <summary>
    /// The Foremen and Superintendents of the recipient crews who hold access to the project. The actor never
    /// receives a notice of their own action.
    /// </summary>
    private static IReadOnlyList<int> NotifyCrews(DelayWorkflowContext context, IReadOnlyList<int> recipientCrewIds) =>
        context.CrewContacts
            .Where(c => recipientCrewIds.Contains(c.CrewId) && c.UserId != context.ActorUserId)
            .Select(c => c.UserId)
            .Distinct()
            .Order()
            .ToList();

    #endregion

    /// <param name="Verb">Completes the refusal "The user cannot {Verb} delay {id}."</param>
    /// <param name="IsPermitted">The actor's role and crew authority. The impact is set only for an acceptance or a send-back.</param>
    /// <param name="CycleOpen">True when the action needs the open cycle; false when it needs the latest cycle closed.</param>
    private sealed record Transition(
        string Verb,
        Func<DelayWorkflowContext, DelayWorkflowImpactRecord?, bool> IsPermitted,
        IReadOnlyCollection<string> FromStatusCodes,
        bool CycleOpen,
        string WrongStatusReason);
}
