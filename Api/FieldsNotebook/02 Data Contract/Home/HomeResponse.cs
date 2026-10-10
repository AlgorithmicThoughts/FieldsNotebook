namespace DataContract.Home
{
    /// <summary>GET /api/projects/{projectId}/home — every count and row comes from the same set of active delays.</summary>
    public sealed record HomeResponse(
        HomeMetricsResponse Metrics,
        IReadOnlyList<DelaySummaryResponse> AttentionDelays,
        AdvanceNoticeResponse? AdvanceNotice,
        IReadOnlyList<ActivityEntryResponse> RecentActivity);

    public sealed record HomeMetricsResponse(int BlockingOurCrews, int CrewsWaitingOnUs, int ReadyToVerify);

    /// <summary>One active delay as a list row shows it. A delay can affect several crews.</summary>
    public sealed record DelaySummaryResponse(
        string Number,
        string Title,
        string StatusCode,
        string TagCode,
        string TagName,
        string OwnerCrewName,
        IReadOnlyList<string> BlockedCrewNames,
        string LocationName,
        string ProjectName,
        bool IsHighPriority);

    /// <param name="ExpectedReadyAt">Tentative completion time in UTC; null until the responsible Superintendent gives one.</param>
    public sealed record AdvanceNoticeResponse(
        string DelayNumber,
        string Title,
        string Description,
        string OwnerCrewName,
        IReadOnlyList<string> BlockedCrewNames,
        DateTimeOffset? ExpectedReadyAt);

    /// <summary>One workflow event. Clients build the timeline text from the event type and values.</summary>
    /// <param name="EventType">ActivityEvents.EventType, such as delay.reopened or delay.handoverVerified.</param>
    /// <param name="AffectedCrewName">The affected crew of a handover check; otherwise null.</param>
    public sealed record ActivityEntryResponse(
        int Id,
        string EventType,
        string DelayNumber,
        string DelayTitle,
        string ActorName,
        string? FromStatusCode,
        string? ToStatusCode,
        DateTimeOffset? NewExpectedReadyAt,
        string? AffectedCrewName,
        DateTimeOffset CreatedAt);
}
