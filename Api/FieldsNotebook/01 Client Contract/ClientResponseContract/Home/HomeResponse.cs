namespace DataContract.Home;

/// <summary>GET /api/projects/{projectId}/home — every count and row comes from the same set of active delays.</summary>
/// <param name="AttentionDelays">The most pressing active delays for the user's crews, at most four.</param>
/// <param name="AdvanceNotice">The advance notice the hero announces, or null.</param>
public sealed record HomeResponse(
    HomeMetricsResponse Metrics,
    IReadOnlyList<DelaySummaryResponse> AttentionDelays,
    DelaySummaryResponse? AdvanceNotice,
    IReadOnlyList<ActivityEntryResponse> RecentActivity);
