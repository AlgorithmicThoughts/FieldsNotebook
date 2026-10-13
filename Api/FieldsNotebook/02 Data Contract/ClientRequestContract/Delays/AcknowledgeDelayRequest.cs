using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <param name="Choice">DelayCycles.AcknowledgmentChoice, the prepared answer the Superintendent selected.</param>
/// <param name="ExpectedReadyAt">Tentative completion time when work remains. Omit it when the work is ready.</param>
public sealed record AcknowledgeDelayRequest(
    [Required, StringLength(30, MinimumLength = 1)] string Choice,
    [StringLength(1000)] string? Note,
    DateTimeOffset? ExpectedReadyAt);
