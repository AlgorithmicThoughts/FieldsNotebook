using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <summary>An affected crew's Foreman sends the proof of work back. The impact comes from the route.</summary>
/// <param name="Reason">ActivityEvents.ReturnReason: what the responsible crew must fix before submitting again.</param>
public sealed record ReturnHandoverRequest(
    [Required, StringLength(1000, MinimumLength = 1)] string Reason);
