namespace DataContract.Home;

/// <summary>One workflow event. Clients build the timeline text from the event type and values.</summary>
/// <param name="EventType">ActivityEvents.EventType, such as delay.reopened or delay.handoverVerified.</param>
/// <param name="AffectedCrewName">The affected crew of a handover check; otherwise null.</param>
public sealed record ActivityEntryResponse(
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
