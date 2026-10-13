namespace DataContract.Domain.Delays;

public sealed record ReportWorkItemRecord(
    int Id,
    string Title,
    IReadOnlyList<int> CrewIds);
