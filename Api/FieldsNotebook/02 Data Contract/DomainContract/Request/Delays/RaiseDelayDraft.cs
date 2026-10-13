namespace DataContract.Domain.Delays;

/// <summary>
/// A Foreman's report as submitted, before the workflow Engine checks it against the project's options.
/// Field lengths, ranges and pairs were already checked on the request contract.
/// </summary>
public sealed record RaiseDelayDraft(
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
    string? LocationName,
    IReadOnlyList<ReportImpactRecord> Impacts,
    IReadOnlyList<ReportPhotoUploadRecord> Photos,
    IReadOnlyList<int> NotifyUserIds);
