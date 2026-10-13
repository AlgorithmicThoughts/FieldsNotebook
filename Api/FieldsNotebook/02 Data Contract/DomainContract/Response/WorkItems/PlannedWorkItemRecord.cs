namespace DataContract.Domain.WorkItems;

/// <param name="ExpectedFinishAt">WorkItems.ExpectedFinishAt, the plan's expected finish; never an acknowledged completion time.</param>
public sealed record PlannedWorkItemRecord(
    int WorkItemId,
    string Title,
    string SegmentName,
    DateTimeOffset PlannedStartAt,
    DateTimeOffset? ExpectedFinishAt);
