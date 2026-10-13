namespace DataContract.Domain.Delays;

public sealed record DelayReportOptionsRecord(
    IReadOnlyList<ReportCrewRecord> Crews,
    IReadOnlyList<ReportWorkItemRecord> WorkItems,
    IReadOnlyList<ReportSegmentRecord> Segments,
    IReadOnlyList<ReportTagRecord> Tags,
    ReportMapRecord? SiteMap,
    IReadOnlyList<ReportPersonRecord> People);
