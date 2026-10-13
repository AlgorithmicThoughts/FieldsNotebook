using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <summary>
/// The outcome of one workflow action: where the delay now stands and the history it appended.
/// Clients refresh the project tracking list for the full delay.
/// </summary>
/// <param name="NoticeCount">Notifications rows written for this event, one per recipient.</param>
public sealed record DelayActionResponse(
    int DelayId,
    string StatusCode,
    int CycleId,
    int ActivityEventId,
    string EventType,
    DateTimeOffset OccurredAt,
    int NoticeCount);
