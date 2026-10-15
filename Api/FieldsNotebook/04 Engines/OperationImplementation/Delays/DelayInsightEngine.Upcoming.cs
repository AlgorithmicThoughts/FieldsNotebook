using Common.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Insights;
using DataContract.Domain.WorkItems;

namespace Engines.Delays;

/// <summary>The unresolved blockers on work that should be under way by the end of the next project day.</summary>
public sealed partial class DelayInsightEngine
{
    // A resolved or withdrawn delay is already out of the active set; a completion time that has
    // passed is not, because only the workflow clears a blocker.
    public IReadOnlyList<BlockedWorkView> BuildUpcoming(ActiveDelaySet active, PlannedWorkSet planned)
    {
        var index = new UpcomingIndex(active, planned);
        return active.Delays
            .SelectMany(index.ImpactsOf, index.BuildRow)
            .OfType<BlockedWorkView>()
            .OrderBy(r => r.WorkItem.PlannedStartAt)
            .ThenBy(r => r.Impact.ImpactId)
            .ToList();
    }

    /// <summary>Active delays and the next project day's planned work, indexed for row building.</summary>
    private sealed class UpcomingIndex
    {
        private readonly ILookup<int, ActiveDelayImpactRecord> _impacts;
        private readonly Dictionary<int, PlannedWorkItemRecord> _plannedById;
        private readonly ILookup<int, WorkItemPredecessorRecord> _predecessors;
        private readonly Dictionary<int, string> _delayNumberByBlockedWorkItem;

        public UpcomingIndex(ActiveDelaySet active, PlannedWorkSet planned)
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

        /// <summary>
        /// One row, or null when the crew already accepted the proof of work or the impact names no work item
        /// the next project day plans for.
        /// </summary>
        public BlockedWorkView? BuildRow(ActiveDelayRecord delay, ActiveDelayImpactRecord impact)
        {
            if (IsStillBlocked(impact) && impact.BlockedWorkItemId is { } workItemId
                && _plannedById.TryGetValue(workItemId, out var workItem))
            {
                return new BlockedWorkView(delay, impact, workItem,
                    IsAdvanceNotice(delay.ReportType),
                    IsHighPriority(delay.CycleCount, delay.HasOpenCycle),
                    CompletionOf(delay, workItemId),
                    BlockedPredecessorOf(workItemId));
            }

            return null;
        }

        #region Private Methods

        // The delay's own tentative time when the responsible crew gave one; otherwise the plan's
        // expected finish for a predecessor, which nobody acknowledged.
        private CompletionEstimateView CompletionOf(ActiveDelayRecord delay, int workItemId)
        {
            if (delay.ExpectedReadyAt is { } readyAt)
            {
                return new CompletionEstimateView(CompletionEstimateSources.DelayCycle, readyAt, null);
            }

            var predecessor = _predecessors[workItemId].FirstOrDefault(p => p.PredecessorExpectedFinishAt is not null);
            if (predecessor is null)
            {
                return new CompletionEstimateView(CompletionEstimateSources.None, null, null);
            }

            return new CompletionEstimateView(
                CompletionEstimateSources.WorkItem, predecessor.PredecessorExpectedFinishAt, predecessor.PredecessorTitle);
        }

        /// <summary>The predecessor a Foreman actually asks about: the one an active delay still holds up.</summary>
        private BlockedPredecessorView? BlockedPredecessorOf(int workItemId)
        {
            return _predecessors[workItemId]
                .Where(p => _delayNumberByBlockedWorkItem.ContainsKey(p.PredecessorWorkItemId))
                .Select(p => new BlockedPredecessorView(p.PredecessorTitle, _delayNumberByBlockedWorkItem[p.PredecessorWorkItemId]))
                .FirstOrDefault();
        }

        #endregion
    }
}
