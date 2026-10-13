namespace DataContract.Reports;

/// <summary>One delay with every identifier resolved to a name, so an exported file reads without the database.</summary>
/// <param name="BlockedCrewNames">Every crew DelayImpacts records for the delay, once each.</param>
/// <param name="ResolvedAt">ClosedAt of the final cycle, which a revoked delay also carries.</param>
/// <param name="CycleCount">More than one cycle means the delay was reopened.</param>
public sealed record DelayReportRowResponse(
    string DelayNumber,
    string ProjectName,
    string Title,
    string StatusName,
    string TagName,
    string ReportType,
    string OwnerCrewName,
    IReadOnlyList<string> BlockedCrewNames,
    string SegmentName,
    string? LocationName,
    string ReportedByDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    int CycleCount,
    int PhotoCount);
