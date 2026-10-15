using DataContract.Home;

namespace DataContract.Delays;

/// <param name="ReturnReason">Why the proof of work was sent back; set only on delay.handoverReturned.</param>
public sealed record DelayEventResponse(
    int Id,
    string EventType,
    string ActorName,
    DateTimeOffset CreatedAt,
    int? CycleSequence,
    DateTimeOffset? OldExpectedReadyAt,
    DateTimeOffset? NewExpectedReadyAt,
    string? ReturnReason);
