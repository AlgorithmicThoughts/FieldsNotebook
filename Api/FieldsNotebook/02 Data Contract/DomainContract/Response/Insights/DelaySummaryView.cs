using DataContract.Domain.Delays;

namespace DataContract.Domain.Insights;

/// <summary>One active delay as a list row shows it, with the priority the delay rules decided.</summary>
public sealed record DelaySummaryView(
    ActiveDelayRecord Delay,
    IReadOnlyList<ActiveDelayImpactRecord> Impacts,
    bool IsHighPriority);
