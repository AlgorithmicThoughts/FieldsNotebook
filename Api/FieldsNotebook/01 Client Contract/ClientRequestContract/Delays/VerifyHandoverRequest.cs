using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

/// <summary>The affected crew's Foreman accepts the handover. The impact comes from the route.</summary>
public sealed record VerifyHandoverRequest(
    [StringLength(1000)] string? Note);
