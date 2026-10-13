namespace DataContract.Domain.Delays;

public sealed record ReportSavedRecord(
    int Id,
    string Number,
    IReadOnlyList<int> ImpactIds);
