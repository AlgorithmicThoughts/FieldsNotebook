namespace DataContract.Domain.Delays;

/// <summary>Active delays that involve a set of crews, with every impact of those delays.</summary>
public sealed record ActiveDelaySet(
    IReadOnlyList<ActiveDelayRecord> Delays,
    IReadOnlyList<ActiveDelayImpactRecord> Impacts)
{
    public static ActiveDelaySet Empty { get; } = new([], []);
}
