using Common.Delays;
using DataContract.Domain.Delays;

namespace Engines.Delays;

/// <summary>
/// How delays read from a crew's point of view. The shared delay rules and the project calendar live
/// here as private rules, so no other Engine is called to answer them.
/// </summary>
public sealed partial class DelayInsightEngine : IDelayInsightEngine
{
    /// <summary>Home shows only the latest handovers; a full timeline belongs to the delay or activity screens.</summary>
    public const int RecentActivityLimitValue = 5;

    /// <summary>Rows in "Needs attention"; the metrics still count every active delay, and Tracking lists the rest.</summary>
    public const int AttentionLimit = 4;

    /// <summary>The site clock every project runs on. Stored timestamps stay UTC; only day boundaries use this zone.</summary>
    private static readonly TimeZoneInfo ProjectTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    public int RecentActivityLimit => RecentActivityLimitValue;

    public DateTimeOffset PlannedWorkCutoff(DateTimeOffset now)
    {
        var dayAfterNext = TimeZoneInfo.ConvertTime(now, ProjectTimeZone).Date.AddDays(2);

        return new DateTimeOffset(dayAfterNext, ProjectTimeZone.GetUtcOffset(dayAfterNext)).ToUniversalTime();
    }

    #region Private Methods

    /// <summary>A reopened delay stays High priority until its reopened cycle closes.</summary>
    private static bool IsHighPriority(int cycleCount, bool hasOpenCycle) => hasOpenCycle && cycleCount > 1;

    private static bool IsAdvanceNotice(string reportType) => reportType == DelayReportTypes.AdvanceNotice;

    /// <summary>
    /// Each affected crew closes the delay for itself: once its Foreman accepts the proof of work in the open
    /// cycle, that crew is no longer blocked, even while other crews are still reviewing.
    /// </summary>
    private static bool IsStillBlocked(ActiveDelayImpactRecord impact) => impact.IsVerifiedInOpenCycle is false;

    /// <summary>The delay waits on a handover check by one of the given affected crews in the current cycle.</summary>
    private static bool AwaitsHandoverCheck(string statusCode, IEnumerable<ActiveDelayImpactRecord> impacts, IReadOnlySet<int> crewIds) =>
        statusCode == DelayStatusCodes.ReadyForHandover
        && impacts.Any(i => crewIds.Contains(i.BlockedCrewId) && i.IsVerifiedInOpenCycle is false);

    #endregion
}
