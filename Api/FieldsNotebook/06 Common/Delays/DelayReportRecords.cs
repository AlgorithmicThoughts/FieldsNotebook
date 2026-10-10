namespace Common.Delays;

public sealed record ReportCrewRecord(int Id, string Name, int ForemanId, int SuperintendentId);
public sealed record ReportWorkItemRecord(int Id, string Title, IReadOnlyList<int> CrewIds);
public sealed record ReportSegmentRecord(int Id, string Name);
public sealed record ReportTagRecord(int Id, string Code, string Name);
public sealed record ReportMapRecord(int Id, string Name, string Uri);
public sealed record ReportPersonRecord(int UserId, string DisplayName, int CrewId, string CrewName, string RoleName);
public sealed record DelayReportOptionsRecord(IReadOnlyList<ReportCrewRecord> Crews,
    IReadOnlyList<ReportWorkItemRecord> WorkItems, IReadOnlyList<ReportSegmentRecord> Segments,
    IReadOnlyList<ReportTagRecord> Tags, ReportMapRecord? SiteMap, IReadOnlyList<ReportPersonRecord> People);
public sealed record ReportImpactRecord(int CrewId, int? WorkItemId);
public sealed record ReportPhotoRecord(string OriginalUrl, string ThumbnailUrl, string FileName,
    decimal? Latitude, decimal? Longitude);
/// <summary>Request-scoped stream sources; keeps HTTP upload types out of Accessors.</summary>
public sealed record ReportPhotoUploadRecord(Func<Stream> OpenOriginal, Func<Stream> OpenThumbnail,
    string ContentType, string FileName, decimal? Latitude, decimal? Longitude);
public sealed record DelayReportRecord(int ProjectId, int ActorUserId, int ReporterCrewId,
    string Title, string Description, int OwnerCrewId, int DelayTagId, string ReportType,
    int? BlockingWorkItemId, int SegmentId, int? SiteMapId, decimal? PinX, decimal? PinY,
    string LocationSource, string? LocationName, DateTimeOffset CreatedAt,
    IReadOnlyList<ReportImpactRecord> Impacts, IReadOnlyList<ReportPhotoUploadRecord> Photos,
    IReadOnlyList<int> RecipientIds);
public sealed record ReportSavedRecord(int Id, string Number, IReadOnlyList<int> ImpactIds);
