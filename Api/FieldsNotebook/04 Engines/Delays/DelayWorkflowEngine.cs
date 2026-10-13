using Common.Delays;
using Common.Errors;
using Common.Results;
using Common.Roles;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Mapping.ClientToDomainMapping;

namespace Engines.Delays
{
    /// <summary>
    /// Who may move a delay and when, from the approved workflow. Each action checks its row of the
    /// transition table, then returns either the writes that carry it out or the reason it is refused.
    /// Nothing here reaches the database or the HTTP contract.
    /// </summary>
    public static class DelayWorkflowEngine
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

        private static readonly Transition RequestHandoverCheckTransition = new("request a handover check on",
            (c, _) => DirectsCrew(c.Access, c.State.OwnerCrewId),
            [DelayStatusCodes.Acknowledged], CycleOpen: true,
            "a handover check follows an acknowledgment.");

        // Only the affected crew's own Foreman can accept the handover for that crew.
        private static readonly Transition VerifyHandoverTransition = new("verify the handover of",
            (c, impact) => LeadsCrew(c.Access, impact!.BlockedCrewId),
            [DelayStatusCodes.ReadyForHandover], CycleOpen: true,
            "the responsible Superintendent has not asked for a handover check yet.");

        private static readonly Transition ResolveTransition = new("resolve",
            (c, _) => DirectsCrew(c.Access, c.State.OwnerCrewId),
            [DelayStatusCodes.ReadyForHandover], CycleOpen: true,
            "a delay resolves after its handover checks.");

        private static readonly Transition ReopenTransition = new("reopen",
            (c, _) => IsSuperintendent(c.Access),
            [DelayStatusCodes.Resolved], CycleOpen: false,
            "only a resolved delay can be reopened.");

        // The Superintendent of the Foreman who raised the delay withdraws it, not the responsible one.
        private static readonly Transition RevokeTransition = new("withdraw",
            (c, _) => DirectsCrew(c.Access, c.State.ReportedByCrewId),
            ActiveStatusCodes, CycleOpen: true,
            "this delay is already closed.");

        /// <summary>The responsible Superintendent answers the delay and may give a tentative completion time.</summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> Acknowledge(DelayWorkflowContext context, AcknowledgeDelayRequest request)
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
            var expectedReadyAt = request.ExpectedReadyAt?.ToUniversalTime();
            var acknowledgment = request.ToDomain();
            var acknowledged = Step(context, DelayEventTypes.Acknowledged, DelayStatusCodes.Acknowledged,
                DelayCycleChange.Acknowledge(state.CycleId, acknowledgment, expectedReadyAt), AffectedCrews(state)) with
            {
                OldExpectedReadyAt = expectedReadyAt is null ? null : state.ExpectedReadyAt,
                NewExpectedReadyAt = expectedReadyAt,
            };

            // When nothing remains for the responsible crew, the same answer asks the affected crews for their
            // handover checks. Each step keeps its own event, so the history still reads as two actions.
            if (DelayAcknowledgmentChoices.IsReadyForHandover(request.Choice))
            {
                return Steps(acknowledged, HandoverCheckStep(context, DelayStatusCodes.Acknowledged));
            }

            return Steps(acknowledged);
        }

        /// <summary>The responsible Superintendent revises the tentative completion time of the active cycle.</summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> ReviseCompletionTime(DelayWorkflowContext context, ReviseCompletionTimeRequest request)
        {
            var state = context.State;
            if (Refusal(ReviseCompletionTimeTransition, context) is { } refused)
            {
                return refused;
            }

            if (request.ExpectedReadyAt is not { } revised)
            {
                return RequestError.InvalidRequest("Enter the revised completion time.");
            }

            var expectedReadyAt = revised.ToUniversalTime();
            // TODO(human): decide what makes a revised completion time acceptable beyond being present.

            // The revision updates the active cycle and appends an event. It never opens another cycle.
            return Steps(Step(context, DelayEventTypes.ExpectedReadyChanged, null,
                DelayCycleChange.ReviseExpectedReady(state.CycleId, expectedReadyAt), AffectedCrews(state)) with
            {
                OldExpectedReadyAt = state.ExpectedReadyAt,
                NewExpectedReadyAt = expectedReadyAt,
            });
        }

        /// <summary>The responsible Superintendent asks the affected crews' Foremen for a handover check.</summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> RequestHandoverCheck(DelayWorkflowContext context)
        {
            if (Refusal(RequestHandoverCheckTransition, context) is { } refused)
            {
                return refused;
            }

            return Steps(HandoverCheckStep(context, context.State.StatusCode));
        }

        /// <summary>The affected crew's Foreman records a successful handover check for the current cycle.</summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> VerifyHandover(DelayWorkflowContext context, int impactId, VerifyHandoverRequest request)
        {
            var state = context.State;
            var impact = state.Impacts.FirstOrDefault(i => i.ImpactId == impactId);
            if (impact is null)
            {
                return DelayErrors.ImpactNotFound(state.DelayId, impactId);
            }

            if (Refusal(VerifyHandoverTransition, context, impact) is { } refused)
            {
                return refused;
            }

            if (impact.IsVerifiedInCycle)
            {
                return DelayErrors.StateConflict(state.DelayId, "that crew already verified the handover in this cycle.");
            }

            return Steps(Step(context, DelayEventTypes.HandoverVerified, null, DelayCycleChange.Touch(state.CycleId), [state.OwnerCrewId]) with
            {
                Verification = request.ToDomain(impact.ImpactId),
            });
        }

        /// <summary>
        /// The responsible Superintendent closes the cycle after the required handover checks, attaching
        /// proof-of-work photos that are stored against the closing cycle.
        /// </summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> Resolve(DelayWorkflowContext context, ResolveDelayRequest request)
        {
            var state = context.State;
            if (Refusal(ResolveTransition, context) is { } refused)
            {
                return refused;
            }

            if (IsAwaitingHandoverCheck(state))
            {
                return DelayErrors.StateConflict(state.DelayId, "every affected crew must verify the handover first.");
            }

            // Every resolution, first or after a reopen, proves the work with its own photos.
            if (DelayPhotoRules.Validate(request.Photos, "Attach one to five proof-of-work photos to resolve the delay.") is { } invalidPhoto)
            {
                return invalidPhoto;
            }

            return Steps(Step(context, DelayEventTypes.Resolved, DelayStatusCodes.Resolved, DelayCycleChange.CloseCycle(state.CycleId), EveryCrew(state)) with
            {
                ProofOfWorkPhotos = request.Photos.ToDomain(),
            });
        }

        /// <summary>A Superintendent reopens a resolved delay, which opens a new cycle for the same delay.</summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> Reopen(DelayWorkflowContext context, ReopenDelayRequest request)
        {
            if (Refusal(ReopenTransition, context) is { } refused)
            {
                return refused;
            }

            // A reopen opens a cycle, which keeps the earlier acknowledgment and handover checks on their own cycle.
            return Steps(Step(context, DelayEventTypes.Reopened, DelayStatusCodes.Reopened, DelayCycleChange.Open(request.Reason), EveryCrew(context.State)));
        }

        /// <summary>The Superintendent of the reporting Foreman withdraws the delay.</summary>
        public static Result<IReadOnlyList<DelayWorkflowWrite>> Revoke(DelayWorkflowContext context)
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

        /// <summary>Some affected crew has not yet verified the handover for the current cycle, or the delay has none.</summary>
        private static bool IsAwaitingHandoverCheck(DelayWorkflowState state)
        {
            return state.Impacts.Count == 0 || state.Impacts.Any(i => i.IsVerifiedInCycle is false);
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
                context.State.StatusCode, toStatusCode, cycle, recipientCrewIds);

        /// <summary>The move to ReadyForHandover, asked for on its own or by an acknowledgment that needs no work.</summary>
        private static DelayWorkflowWrite HandoverCheckStep(DelayWorkflowContext context, string fromStatusCode) =>
            Step(context, DelayEventTypes.ReadyForHandover, DelayStatusCodes.ReadyForHandover,
                DelayCycleChange.Touch(context.State.CycleId), AffectedCrews(context.State)) with
            {
                FromStatusCode = fromStatusCode,
            };

        /// <summary>Every affected crew of the delay. The list is never reduced to one.</summary>
        private static IReadOnlyList<int> AffectedCrews(DelayWorkflowState state) =>
            state.Impacts.Select(i => i.BlockedCrewId).Distinct().ToList();

        private static IReadOnlyList<int> EveryCrew(DelayWorkflowState state) =>
            AffectedCrews(state).Append(state.OwnerCrewId).Distinct().ToList();

        #endregion

        /// <param name="Verb">Completes the refusal "The user cannot {Verb} delay {id}."</param>
        /// <param name="IsPermitted">The actor's role and crew authority. The impact is set only for a handover verification.</param>
        /// <param name="CycleOpen">True when the action needs the open cycle; false when it needs the latest cycle closed.</param>
        private sealed record Transition(
            string Verb,
            Func<DelayWorkflowContext, DelayWorkflowImpactRecord?, bool> IsPermitted,
            IReadOnlyCollection<string> FromStatusCodes,
            bool CycleOpen,
            string WrongStatusReason);
    }
}
