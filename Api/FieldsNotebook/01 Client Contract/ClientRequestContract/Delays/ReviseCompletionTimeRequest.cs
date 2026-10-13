using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <summary>A revised tentative completion time. It updates the active cycle and never opens a new one.</summary>
/// <param name="ExpectedReadyAt">Nullable only so an omitted value fails [Required] instead of binding to 0001-01-01.</param>
public sealed record ReviseCompletionTimeRequest(
    [Required] DateTimeOffset? ExpectedReadyAt);
