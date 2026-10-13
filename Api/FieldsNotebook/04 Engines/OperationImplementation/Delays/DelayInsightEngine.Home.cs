using DataContract.Domain.Activity;
using DataContract.Domain.Delays;
using DataContract.Domain.Insights;

namespace Engines.Delays;

/// <summary>What the home screen shows a user about the active delays involving their crews.</summary>
public sealed partial class DelayInsightEngine
{
    public HomeView BuildHome(IReadOnlySet<int> ourCrewIds, ActiveDelaySet active, IReadOnlyList<ActivityEventRecord> activity)
    {
        var index = new CrewDelayIndex(active, ourCrewIds);
        var attention = index.Delays
            .Select(d => BuildSummary(d, index))
            .OrderByDescending(d => d.IsHighPriority)
            .Take(AttentionLimit)
            .ToList();

        return new HomeView(BuildMetrics(index), attention, BuildAdvanceNotice(index), activity);
    }

    #region Private Methods

    private static HomeMetricsView BuildMetrics(CrewDelayIndex index)
    {
        var blockingOurCrews = index.Delays.Count(index.BlocksOurCrews);
        var crewsWaitingOnUs = index.Delays
            .Where(d => index.OurCrewIds.Contains(d.OwnerCrewId))
            .SelectMany(d => index.ImpactsOf(d).Where(i => i.BlockedCrewId != d.OwnerCrewId))
            .Select(i => i.BlockedCrewId)
            .Distinct()
            .Count();
        var readyToVerify = index.Delays.Count(d => AwaitsHandoverCheck(d.StatusCode, index.ImpactsOf(d), index.OurCrewIds));

        return new HomeMetricsView(blockingOurCrews, crewsWaitingOnUs, readyToVerify);
    }

    // The earliest tentative completion first; a notice without an estimate still shows, after the others.
    private static DelaySummaryView? BuildAdvanceNotice(CrewDelayIndex index)
    {
        return index.Delays
            .Where(d => IsAdvanceNotice(d.ReportType) && index.BlocksOurCrews(d))
            .OrderBy(d => d.ExpectedReadyAt is null)
            .ThenBy(d => d.ExpectedReadyAt)
            .Select(d => BuildSummary(d, index))
            .FirstOrDefault();
    }

    private static DelaySummaryView BuildSummary(ActiveDelayRecord delay, CrewDelayIndex index)
    {
        return new DelaySummaryView(delay, index.ImpactsOf(delay).ToList(), IsHighPriority(delay.CycleCount, delay.HasOpenCycle));
    }

    #endregion

    /// <summary>Active delays indexed by delay, from the point of view of the user's crews.</summary>
    private sealed class CrewDelayIndex(ActiveDelaySet set, IReadOnlySet<int> ourCrewIds)
    {
        private readonly ILookup<int, ActiveDelayImpactRecord> _impacts = set.Impacts.ToLookup(i => i.DelayId);

        public IReadOnlyList<ActiveDelayRecord> Delays { get; } = set.Delays;

        public IReadOnlySet<int> OurCrewIds { get; } = ourCrewIds;

        public IEnumerable<ActiveDelayImpactRecord> ImpactsOf(ActiveDelayRecord delay)
        {
            return _impacts[delay.DelayId];
        }

        public bool BlocksOurCrews(ActiveDelayRecord delay)
        {
            return ImpactsOf(delay).Any(i => OurCrewIds.Contains(i.BlockedCrewId));
        }
    }
}
