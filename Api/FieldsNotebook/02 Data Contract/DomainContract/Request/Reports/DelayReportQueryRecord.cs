namespace DataContract.Domain.Reports;

/// <summary>The delays a report reads: created at or after <paramref name="CreatedFrom"/> and before <paramref name="CreatedBefore"/>.</summary>
/// <param name="CreatedFrom">Inclusive lower bound in UTC; null leaves that end open.</param>
/// <param name="CreatedBefore">Exclusive upper bound in UTC; null leaves that end open.</param>
public sealed record DelayReportQueryRecord(
    IReadOnlyList<int> ProjectIds,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedBefore);
