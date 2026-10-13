namespace DataContract.Home;

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
