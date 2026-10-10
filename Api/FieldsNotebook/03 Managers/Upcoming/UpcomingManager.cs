using Accessors.Delays;
using Accessors.Projects;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.WorkItems;
using DataContract.Upcoming;
using Engines.Crews;
using Engines.Delays;
using Engines.Projects;

namespace Managers.Upcoming
{
    public class UpcomingManager(
        IProjectAccessAccessor projectAccessAccessor,
        IDelayAccessor delayAccessor,
        IWorkItemAccessor workItemAccessor) : IUpcomingManager
    {
        public async Task<UpcomingResponse> GetUpcomingAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);

            var access = await projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken)
                ?? throw new ProjectAccessDeniedException(projectId);
            var ourCrewIds = CrewCoverageEngine.GetCoveredCrews(access.RoleName, access.Crews)
                .Select(c => c.CrewId)
                .ToHashSet();
            if (ourCrewIds.Count == 0)
            {
                return new UpcomingResponse([]);
            }

            // Sequential on purpose: both Accessors share this request's DbContext.
            var active = await delayAccessor.GetActiveDelaysForCrewsAsync(projectId, ourCrewIds, cancellationToken);
            if (active.Delays.Count == 0)
            {
                return new UpcomingResponse([]);
            }

            var planned = await workItemAccessor.GetPlannedWorkAsync(
                projectId, ProjectCalendarEngine.EndOfNextProjectDay(DateTimeOffset.UtcNow), cancellationToken);

            return new UpcomingResponse(BuildBlockedWork(active, planned));
        }

        // A resolved or withdrawn delay is already out of the active set; a completion time that has
        // passed is not, because only the workflow clears a blocker.
        private static IReadOnlyList<BlockedWorkResponse> BuildBlockedWork(ActiveDelaySet active, PlannedWorkSet planned)
        {
            var view = new UpcomingView(active, planned);

            return active.Delays
                .SelectMany(d => view.ImpactsOf(d), (d, i) => view.BuildRow(d, i))
                .OfType<BlockedWorkResponse>()
                .OrderBy(r => r.PlannedStartAt)
                .ThenBy(r => r.Id)
                .ToList();
        }

        /// <summary>Active delays and the next project day's planned work, indexed for row building.</summary>
        private sealed class UpcomingView
        {
            private readonly ILookup<int, ActiveDelayImpactRecord> _impacts;
            private readonly Dictionary<int, PlannedWorkItemRecord> _plannedById;
            private readonly ILookup<int, WorkItemPredecessorRecord> _predecessors;
            private readonly Dictionary<int, string> _delayNumberByBlockedWorkItem;

            public UpcomingView(ActiveDelaySet active, PlannedWorkSet planned)
            {
                _impacts = active.Impacts.ToLookup(i => i.DelayId);
                _plannedById = planned.WorkItems.ToDictionary(w => w.WorkItemId);
                _predecessors = planned.Predecessors.ToLookup(p => p.SuccessorWorkItemId);
                _delayNumberByBlockedWorkItem = active.Delays
                    .SelectMany(d => _impacts[d.DelayId], (d, i) => (d.Number, i.BlockedWorkItemId))
                    .Where(x => x.BlockedWorkItemId is not null)
                    .GroupBy(x => x.BlockedWorkItemId!.Value)
                    .ToDictionary(g => g.Key, g => g.First().Number);
            }

            public IEnumerable<ActiveDelayImpactRecord> ImpactsOf(ActiveDelayRecord delay) => _impacts[delay.DelayId];

            /// <summary>One row, or null when the impact names no work item the next project day plans for.</summary>
            public BlockedWorkResponse? BuildRow(ActiveDelayRecord delay, ActiveDelayImpactRecord impact)
            {
                if (impact.BlockedWorkItemId is not { } workItemId
                    || !_plannedById.TryGetValue(workItemId, out var workItem))
                {
                    return null;
                }

                return new BlockedWorkResponse(
                    impact.ImpactId,
                    workItem.Title,
                    workItem.SegmentName,
                    impact.BlockedCrewName,
                    workItem.PlannedStartAt,
                    delay.Number,
                    delay.Title,
                    delay.OwnerCrewName,
                    delay.TagName,
                    DelayRulesEngine.IsAdvanceNotice(delay.ReportType),
                    DelayRulesEngine.IsHighPriority(delay.CycleCount, delay.HasOpenCycle),
                    CompletionOf(delay, workItemId),
                    BlockedPredecessorOf(workItemId));
            }

            // The delay's own tentative time when the responsible crew gave one; otherwise the plan's
            // expected finish for a predecessor, which nobody acknowledged.
            private CompletionEstimateResponse CompletionOf(ActiveDelayRecord delay, int workItemId)
            {
                if (delay.ExpectedReadyAt is { } readyAt)
                {
                    return new CompletionEstimateResponse(CompletionEstimateSources.DelayCycle, readyAt, null);
                }

                var predecessor = _predecessors[workItemId].FirstOrDefault(p => p.PredecessorExpectedFinishAt is not null);

                return predecessor is null
                    ? new CompletionEstimateResponse(CompletionEstimateSources.None, null, null)
                    : new CompletionEstimateResponse(
                        CompletionEstimateSources.WorkItem, predecessor.PredecessorExpectedFinishAt, predecessor.PredecessorTitle);
            }

            /// <summary>The predecessor a Foreman actually asks about: the one an active delay still holds up.</summary>
            private BlockedPredecessorResponse? BlockedPredecessorOf(int workItemId) =>
                _predecessors[workItemId]
                    .Where(p => _delayNumberByBlockedWorkItem.ContainsKey(p.PredecessorWorkItemId))
                    .Select(p => new BlockedPredecessorResponse(
                        p.PredecessorTitle, _delayNumberByBlockedWorkItem[p.PredecessorWorkItemId]))
                    .FirstOrDefault();
        }
    }
}
