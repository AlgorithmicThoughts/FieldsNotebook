namespace DataContract.Home
{
    /// <summary>GET /api/projects/{projectId}/home — every count and row comes from the same set of active delays.</summary>
    public sealed record HomeResponse(
        HomeMetricsResponse Metrics,
        IReadOnlyList<DelaySummaryResponse> AttentionDelays,
        AdvanceNoticeResponse? AdvanceNotice);

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
}
