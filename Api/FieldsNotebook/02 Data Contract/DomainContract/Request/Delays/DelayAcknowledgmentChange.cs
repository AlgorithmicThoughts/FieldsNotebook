namespace DataContract.Domain.Delays;

/// <summary>The first acknowledgment of a cycle. A later action never overwrites it.</summary>
public sealed record DelayAcknowledgmentChange(
    string Choice,
    string? Note);
