using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <param name="Choice">DelayCycles.AcknowledgmentChoice, the prepared answer the Superintendent selected.</param>
/// <param name="ExpectedReadyAt">Tentative completion time when work remains. Omit it when the work is ready.</param>
public sealed record AcknowledgeDelayRequest(
    [Required, StringLength(30, MinimumLength = 1)] string Choice,
    [StringLength(1000)] string? Note,
    DateTimeOffset? ExpectedReadyAt);

/// <summary>A revised tentative completion time. It updates the active cycle and never opens a new one.</summary>
public sealed record ReviseCompletionTimeRequest(DateTimeOffset ExpectedReadyAt);

/// <param name="ImpactId">DelayImpacts.Id for the affected crew and work item this Foreman verifies.</param>
public sealed record VerifyHandoverRequest(
    [Range(1, int.MaxValue)] int ImpactId,
    [StringLength(1000)] string? Note);

/// <param name="Reason">DelayCycles.OpenReason for the new cycle. A reopen always records one.</param>
public sealed record ReopenDelayRequest(
    [Required, StringLength(1000, MinimumLength = 1)] string Reason);

/// <summary>
/// The outcome of one workflow action: where the delay now stands and the history it appended.
/// Clients refresh the project tracking list for the full delay.
/// </summary>
/// <param name="NoticeCount">Notifications rows written for this event, one per recipient.</param>
public sealed record DelayActionResponse(int DelayId, string StatusCode, int CycleId, int ActivityEventId,
    string EventType, DateTimeOffset OccurredAt, int NoticeCount);
