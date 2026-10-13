using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <param name="Reason">DelayCycles.OpenReason for the new cycle. A reopen always records one.</param>
public sealed record ReopenDelayRequest(
    [Required, StringLength(1000, MinimumLength = 1)] string Reason);
