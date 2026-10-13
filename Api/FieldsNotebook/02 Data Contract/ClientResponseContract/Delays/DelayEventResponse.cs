using DataContract.Home;

namespace DataContract.Delays;

public sealed record DelayEventResponse(
    int Id,
    string EventType,
    string ActorName,
    DateTimeOffset CreatedAt,
    int? CycleSequence,
    DateTimeOffset? OldExpectedReadyAt,
    DateTimeOffset? NewExpectedReadyAt);
