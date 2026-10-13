namespace DataContract.Domain.Activity;

/// <param name="AffectedCrewName">The affected crew of the event's impact, for a handover check; otherwise null.</param>
public sealed record ActivityEventRecord(
    int Id,
    string EventType,
    string DelayNumber,
    string DelayTitle,
    string ActorName,
    string? FromStatusCode,
    string? ToStatusCode,
    DateTimeOffset? NewExpectedReadyAt,
    string? AffectedCrewName,
    DateTimeOffset CreatedAt);
