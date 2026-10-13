namespace DataContract.Delays;

public sealed record ReportWorkItemResponse(
    int Id,
    string Title,
    IReadOnlyList<int> CrewIds);
