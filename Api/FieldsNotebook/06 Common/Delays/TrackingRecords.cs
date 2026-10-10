namespace Common.Delays;

public sealed record TrackingSet(
    IReadOnlyList<TrackingDelayRecord> Delays,
    IReadOnlyList<TrackingImpactRecord> Impacts,
    IReadOnlyList<TrackingCycleRecord> Cycles,
    IReadOnlyList<TrackingVerificationRecord> Verifications,
    IReadOnlyList<TrackingPhotoRecord> Photos,
    IReadOnlyList<TrackingEventRecord> Events,
    IReadOnlyList<TrackingSiteMapRecord> SiteMaps)
{
    public static TrackingSet Empty { get; } = new([], [], [], [], [], [], []);
}

public sealed record TrackingDelayRecord(ActiveDelayRecord Summary, int ReporterId, string ReporterName, string ReporterCrewName);
public sealed record TrackingImpactRecord(int Id, int DelayId, string CrewName, int? WorkItemId, string? WorkItemTitle);
public sealed record TrackingCycleRecord(int Id, int DelayId, DateTimeOffset CreatedAt, string? OpenReason,
    string? AcknowledgmentChoice, string? AcknowledgmentNote, DateTimeOffset? AcknowledgedAt,
    string? AcknowledgedByName, DateTimeOffset? ExpectedReadyAt, DateTimeOffset? ClosedAt);
public sealed record TrackingVerificationRecord(int ImpactId, int CycleId, string VerifiedByName, DateTimeOffset VerifiedAt, string? Note);
public sealed record TrackingPhotoRecord(int Id, int DelayId, string Purpose, string OriginalUrl, string ThumbnailUrl,
    string FileName, DateTimeOffset CreatedAt, decimal? Latitude, decimal? Longitude);
public sealed record TrackingEventRecord(int Id, int DelayId, int CycleId, string EventType, string ActorName,
    DateTimeOffset CreatedAt, DateTimeOffset? OldExpectedReadyAt, DateTimeOffset? NewExpectedReadyAt);
public sealed record TrackingSiteMapRecord(int Id, string Name, string Uri);