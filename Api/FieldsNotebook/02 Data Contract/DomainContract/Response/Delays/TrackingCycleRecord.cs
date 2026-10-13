namespace DataContract.Domain.Delays;

public sealed record TrackingCycleRecord(
    int Id,
    int DelayId,
    DateTimeOffset CreatedAt,
    string? OpenReason,
    string? AcknowledgmentChoice,
    string? AcknowledgmentNote,
    DateTimeOffset? AcknowledgedAt,
    string? AcknowledgedByName,
    DateTimeOffset? ExpectedReadyAt,
    DateTimeOffset? ClosedAt);
