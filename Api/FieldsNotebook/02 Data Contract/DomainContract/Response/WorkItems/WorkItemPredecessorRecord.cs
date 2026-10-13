namespace DataContract.Domain.WorkItems;

/// <summary>One WorkItemDependencies row: the predecessor must hand over before the successor can start.</summary>
/// <param name="PredecessorExpectedFinishAt">Carried here so a predecessor outside the planned set still supplies its expected finish.</param>
public sealed record WorkItemPredecessorRecord(
    int SuccessorWorkItemId,
    int PredecessorWorkItemId,
    string PredecessorTitle,
    DateTimeOffset? PredecessorExpectedFinishAt);
