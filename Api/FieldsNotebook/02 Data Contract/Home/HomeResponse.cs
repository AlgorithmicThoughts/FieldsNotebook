namespace DataContract.Home
{
    /// <summary>GET /api/projects/{projectId}/home — every count and row comes from the same set of active delays.</summary>
    /// <param name="AttentionDelays">The most pressing active delays for the user's crews, at most four.</param>
    /// <param name="AdvanceNotice">The advance notice the hero announces, or null.</param>
    public sealed record HomeResponse(
        HomeMetricsResponse Metrics,
        IReadOnlyList<DelaySummaryResponse> AttentionDelays,
        DelaySummaryResponse? AdvanceNotice,
        IReadOnlyList<ActivityEntryResponse> RecentActivity);

    /// <summary>Counts across every active delay for the user's crews, not only the rows shown.</summary>
    public sealed record HomeMetricsResponse(int BlockingOurCrews, int CrewsWaitingOnUs, int ReadyToVerify);

    /// <summary>One delay as a list row shows it. A delay can affect several crews.</summary>
    /// <param name="LocationName">Delays.LocationName, or the segment name when none was typed.</param>
    /// <param name="ExpectedReadyAt">Tentative completion time of the open cycle in UTC, or null.</param>
    public sealed record DelaySummaryResponse(
        int Id,
        string Number,
        string Title,
        string StatusCode,
        string TagCode,
        string TagName,
        string OwnerCrewName,
        IReadOnlyList<string> BlockedCrewNames,
        IReadOnlyList<DelayImpactResponse> Impacts,
        string LocationName,
        int ProjectId,
        string ProjectName,
        bool IsHighPriority,
        string ReportType,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpectedReadyAt,
        DelayPinResponse? Pin,
        int? SiteMapId,
        string? SegmentName);

    /// <param name="WorkItemId">DelayImpacts.BlockedWorkItemId, or null when no work item was named.</param>
    public sealed record DelayImpactResponse(int Id, string CrewName, int? WorkItemId);

    /// <summary>Normalized position from zero to one on the delay's site map image.</summary>
    public sealed record DelayPinResponse(decimal X, decimal Y);

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
