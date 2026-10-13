using DataContract.Home;

namespace DataContract.Delays;

public sealed record DelayCycleResponse(
    int Id,
    int Sequence,
    DateTimeOffset CreatedAt,
    string? OpenReason,
    string? AcknowledgmentChoice,
    string? AcknowledgmentNote,
    DateTimeOffset? AcknowledgedAt,
    string? AcknowledgedByName,
    DateTimeOffset? ExpectedReadyAt,
    DateTimeOffset? ClosedAt);
