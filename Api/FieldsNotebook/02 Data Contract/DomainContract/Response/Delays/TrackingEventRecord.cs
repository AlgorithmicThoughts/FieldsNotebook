namespace DataContract.Domain.Delays;

public sealed record TrackingEventRecord(
    int Id,
    int DelayId,
    int CycleId,
    string EventType,
    string ActorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? OldExpectedReadyAt,
    DateTimeOffset? NewExpectedReadyAt,
    string? ReturnReason);
