using DataContract.Domain.Delays;
using DataContract.Domain.WorkItems;
using DataContract.Upcoming;
using Engines.Delays;

namespace Engines.Upcoming;

/// <summary>The unresolved blockers on work that should be under way by the end of the next project day.</summary>
public static class UpcomingEngine
{
    // A resolved or withdrawn delay is already out of the active set; a completion time that has
    // passed is not, because only the workflow clears a blocker.
    public static UpcomingResponse BuildUpcoming(ActiveDelaySet active, PlannedWorkSet planned)
    {
        var view = new UpcomingView(active, planned);
        var blockedWork = active.Delays
            .SelectMany(view.ImpactsOf, view.BuildRow)
            .OfType<BlockedWorkResponse>()
            .OrderBy(r => r.PlannedStartAt)
            .ThenBy(r => r.Id)
            .ToList();

        return new UpcomingResponse(blockedWork);
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

        public IEnumerable<ActiveDelayImpactRecord> ImpactsOf(ActiveDelayRecord delay)
        {
            return _impacts[delay.DelayId];
        }

        /// <summary>One row, or null when the impact names no work item the next project day plans for.</summary>
        public BlockedWorkResponse? BuildRow(ActiveDelayRecord delay, ActiveDelayImpactRecord impact)
        {
            if (impact.BlockedWorkItemId is { } workItemId && _plannedById.TryGetValue(workItemId, out var workItem))
            {
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

            return null;
        }

        #region Private Methods

        // The delay's own tentative time when the responsible crew gave one; otherwise the plan's
        // expected finish for a predecessor, which nobody acknowledged.
        private CompletionEstimateResponse CompletionOf(ActiveDelayRecord delay, int workItemId)
        {
            if (delay.ExpectedReadyAt is { } readyAt)
            {
                return new CompletionEstimateResponse(CompletionEstimateSources.DelayCycle, readyAt, null);
            }

            var predecessor = _predecessors[workItemId].FirstOrDefault(p => p.PredecessorExpectedFinishAt is not null);
            if (predecessor is null)
            {
                return new CompletionEstimateResponse(CompletionEstimateSources.None, null, null);
            }

            return new CompletionEstimateResponse(
                CompletionEstimateSources.WorkItem, predecessor.PredecessorExpectedFinishAt, predecessor.PredecessorTitle);
        }

        /// <summary>The predecessor a Foreman actually asks about: the one an active delay still holds up.</summary>
        private BlockedPredecessorResponse? BlockedPredecessorOf(int workItemId)
        {
            return _predecessors[workItemId]
                .Where(p => _delayNumberByBlockedWorkItem.ContainsKey(p.PredecessorWorkItemId))
                .Select(p => new BlockedPredecessorResponse(p.PredecessorTitle, _delayNumberByBlockedWorkItem[p.PredecessorWorkItemId]))
                .FirstOrDefault();
        }

        #endregion
    }
}
