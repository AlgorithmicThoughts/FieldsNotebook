namespace DataContract.Domain.Reports;

/// <summary>The delays in a report scope and the crews each one blocks.</summary>
public sealed record DelayReportSet(
    IReadOnlyList<DelayReportRecord> Delays,
    IReadOnlyList<DelayReportImpactRecord> Impacts)
{
    public static DelayReportSet Empty { get; } = new([], []);
}
