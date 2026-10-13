using DataContract.Domain.Activity;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
using DataContract.Mapping.DomainToClientMapping;
using Engines.Delays;

namespace Engines.Home;

/// <summary>What the home screen shows a user about the active delays involving their crews.</summary>
public static class HomeEngine
{
    /// <summary>Home shows only the latest handovers; a full timeline belongs to the delay or activity screens.</summary>
    public const int RecentActivityLimit = 5;

    /// <summary>Rows in "Needs attention"; the metrics still count every active delay, and Tracking lists the rest.</summary>
    public const int AttentionLimit = 4;

    public static HomeResponse BuildHome(ProjectAccessRecord access, IReadOnlySet<int> ourCrewIds, ActiveDelaySet active,
        IReadOnlyList<ActivityEventRecord> activity)
    {
        var view = new ActiveDelayView(active, ourCrewIds);
        var attention = view.Delays
            .Select(d => BuildSummary(d, view, access))
            .OrderByDescending(d => d.IsHighPriority)
            .Take(AttentionLimit)
            .ToList();

        return new HomeResponse(BuildMetrics(view), attention, BuildAdvanceNotice(view, access), activity.ToResponse());
    }

    #region Private Methods

    private static HomeMetricsResponse BuildMetrics(ActiveDelayView view)
    {
        var blockingOurCrews = view.Delays.Count(view.BlocksOurCrews);
        var crewsWaitingOnUs = view.Delays
            .Where(d => view.OurCrewIds.Contains(d.OwnerCrewId))
            .SelectMany(d => view.ImpactsOf(d).Where(i => i.BlockedCrewId != d.OwnerCrewId))
            .Select(i => i.BlockedCrewId)
            .Distinct()
            .Count();
        var readyToVerify = view.Delays.Count(d =>
            DelayRulesEngine.AwaitsHandoverCheck(d.StatusCode, view.ImpactsOf(d), view.OurCrewIds));

        return new HomeMetricsResponse(blockingOurCrews, crewsWaitingOnUs, readyToVerify);
    }

    // The earliest tentative completion first; a notice without an estimate still shows, after the others.
    private static DelaySummaryResponse? BuildAdvanceNotice(ActiveDelayView view, ProjectAccessRecord access)
    {
        return view.Delays
            .Where(d => DelayRulesEngine.IsAdvanceNotice(d.ReportType) && view.BlocksOurCrews(d))
            .OrderBy(d => d.ExpectedReadyAt is null)
            .ThenBy(d => d.ExpectedReadyAt)
            .Select(d => BuildSummary(d, view, access))
            .FirstOrDefault();
    }

    private static DelaySummaryResponse BuildSummary(ActiveDelayRecord delay, ActiveDelayView view, ProjectAccessRecord access)
    {
        return delay.ToSummaryResponse(view.ImpactsOf(delay).Select(i => i.ToResponse()), access,
            DelayRulesEngine.IsHighPriority(delay.CycleCount, delay.HasOpenCycle));
    }

    #endregion

    /// <summary>Active delays indexed by delay, from the point of view of the user's crews.</summary>
    private sealed class ActiveDelayView(ActiveDelaySet set, IReadOnlySet<int> ourCrewIds)
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
