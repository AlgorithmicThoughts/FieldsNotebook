namespace DataContract.Domain.Reports;

/// <param name="FinalCycleClosedAt">ClosedAt of the delay's latest cycle, or null while that cycle is open.</param>
public sealed record DelayReportRecord(
    int DelayId,
    string Number,
    string ProjectName,
    string Title,
    string StatusName,
    string TagName,
    string ReportType,
    string OwnerCrewName,
    string SegmentName,
    string? LocationName,
    string ReportedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinalCycleClosedAt,
    int CycleCount,
    int PhotoCount);
