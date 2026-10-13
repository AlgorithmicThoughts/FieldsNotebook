namespace DataContract.Domain.Delays;

public sealed record ReportImpactRecord(
    int CrewId,
    int? WorkItemId);
