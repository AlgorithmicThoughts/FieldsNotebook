using DataContract.Domain.Activity;

namespace DataContract.Domain.Insights;

/// <summary>What the home screen shows a user about the active delays involving their crews.</summary>
/// <param name="Attention">The most pressing active delays, at most the attention limit.</param>
/// <param name="AdvanceNotice">The advance notice the hero announces, or null.</param>
public sealed record HomeView(
    HomeMetricsView Metrics,
    IReadOnlyList<DelaySummaryView> Attention,
    DelaySummaryView? AdvanceNotice,
    IReadOnlyList<ActivityEventRecord> RecentActivity);

/// <summary>Counts across every active delay for the user's crews, not only the rows shown.</summary>
public sealed record HomeMetricsView(
    int BlockingOurCrews,
    int CrewsWaitingOnUs,
    int ReadyToVerify);
