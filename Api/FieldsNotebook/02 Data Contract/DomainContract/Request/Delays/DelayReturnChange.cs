namespace DataContract.Domain.Delays;

/// <summary>An affected crew's Foreman sends the proof of work back for their own impact, saying why.</summary>
public sealed record DelayReturnChange(
    int ImpactId,
    string Reason);
