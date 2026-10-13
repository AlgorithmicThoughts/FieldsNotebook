namespace DataContract.Delays;

public sealed record DelayReportOptionsResponse(
    int ProjectId,
    string ProjectName,
    IReadOnlyList<ReportCrewResponse> Crews,
    IReadOnlyList<ReportWorkItemResponse> WorkItems,
    IReadOnlyList<ReportSegmentResponse> Segments,
    IReadOnlyList<ReportTagResponse> Tags,
    TrackingSiteMapResponse? SiteMap,
    IReadOnlyList<ReportPersonResponse> People);
