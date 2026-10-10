using DataContract.Home;

namespace DataContract.Delays;

public sealed record TrackingResponse(int ProjectId, string ProjectName, IReadOnlyList<TrackingDelayResponse> Delays);
public sealed record TrackingDelayResponse(DelaySummaryResponse Summary, string Description, DelayReporterResponse ReportedBy,
    IReadOnlyList<DelayCycleResponse> Cycles, IReadOnlyList<DelayPhotoGroupResponse> Photos,
    IReadOnlyList<TrackingImpactResponse> Impacts, IReadOnlyList<DelayEventResponse> Events, TrackingSiteMapResponse? SiteMap);
public sealed record DelayReporterResponse(int UserId, string DisplayName, string CrewName, DateTimeOffset ReportedAt);
public sealed record DelayCycleResponse(int Id, int Sequence, DateTimeOffset CreatedAt, string? OpenReason,
    string? AcknowledgmentChoice, string? AcknowledgmentNote, DateTimeOffset? AcknowledgedAt, string? AcknowledgedByName,
    DateTimeOffset? ExpectedReadyAt, DateTimeOffset? ClosedAt);
public sealed record DelayVerificationResponse(string VerifiedByName, DateTimeOffset VerifiedAt, string? Note);
public sealed record TrackingImpactResponse(int Id, string CrewName, int? WorkItemId, string? WorkItemTitle,
    IReadOnlyList<DelayVerificationResponse> Verifications);
public sealed record DelayPhotoResponse(int Id, string OriginalUrl, string ThumbnailUrl, string FileName,
    DateTimeOffset CreatedAt, decimal? Latitude, decimal? Longitude);
public sealed record DelayPhotoGroupResponse(string Purpose, IReadOnlyList<DelayPhotoResponse> Photos);
public sealed record DelayEventResponse(int Id, string EventType, string ActorName, DateTimeOffset CreatedAt,
    int? CycleSequence, DateTimeOffset? OldExpectedReadyAt, DateTimeOffset? NewExpectedReadyAt);
public sealed record TrackingSiteMapResponse(int Id, string Name, string Uri);