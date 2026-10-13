namespace DataContract.Domain.Delays;

/// <summary>A new delay as the reporting Foreman raises it, with its impacts, photos and first notices.</summary>
/// <param name="Photos">The uploads the Manager stores before the write; the Accessor records the stored copies it is given.</param>
/// <param name="EventType">The ActivityEvents.EventType that records the report, decided by the workflow Engine.</param>
public sealed record RaiseDelayWrite(
    int ProjectId,
    int ActorUserId,
    int ReporterCrewId,
    string Title,
    string Description,
    int OwnerCrewId,
    int DelayTagId,
    string ReportType,
    int? BlockingWorkItemId,
    int SegmentId,
    int? SiteMapId,
    decimal? PinX,
    decimal? PinY,
    string LocationSource,
    string? LocationName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ReportImpactRecord> Impacts,
    IReadOnlyList<ReportPhotoUploadRecord> Photos,
    IReadOnlyList<int> RecipientIds,
    string EventType);
