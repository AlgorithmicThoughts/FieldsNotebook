namespace DataContract.Domain.Delays;

public sealed record ReportCrewRecord(
    int Id,
    string Name,
    int ForemanId,
    int SuperintendentId);
