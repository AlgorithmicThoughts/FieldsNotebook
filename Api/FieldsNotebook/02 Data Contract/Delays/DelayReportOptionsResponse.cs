namespace DataContract.Delays;

public sealed record DelayReportOptionsResponse(
    int ProjectId, 
    string ProjectName,
    IReadOnlyList<ReportCrewResponse> Crews, 
    IReadOnlyList<ReportWorkItemResponse> WorkItems,
    IReadOnlyList<ReportSegmentResponse> Segments, 
    IReadOnlyList<ReportTagResponse> Tags,
    TrackingSiteMapResponse? SiteMap, 
    IReadOnlyList<ReportPersonResponse> People
    );
    
public sealed record ReportCrewResponse(int Id, string Name);
public sealed record ReportWorkItemResponse(int Id, string Title, IReadOnlyList<int> CrewIds);
public sealed record ReportSegmentResponse(int Id, string Name);
public sealed record ReportTagResponse(int Id, string Code, string Name);
public sealed record ReportPersonResponse(int UserId, string DisplayName, int CrewId, string CrewName, string RoleName);
