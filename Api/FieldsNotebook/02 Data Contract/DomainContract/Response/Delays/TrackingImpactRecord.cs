namespace DataContract.Domain.Delays;

public sealed record TrackingImpactRecord(
    int Id,
    int DelayId,
    string CrewName,
    int? WorkItemId,
    string? WorkItemTitle);
