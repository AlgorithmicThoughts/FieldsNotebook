namespace DataContract.Domain.WorkItems;

/// <summary>Unfinished planned work in a project, with the predecessors each of those work items waits on.</summary>
public sealed record PlannedWorkSet(
    IReadOnlyList<PlannedWorkItemRecord> WorkItems,
    IReadOnlyList<WorkItemPredecessorRecord> Predecessors)
{
    public static PlannedWorkSet Empty { get; } = new([], []);
}
