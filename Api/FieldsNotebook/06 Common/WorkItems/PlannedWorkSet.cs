namespace Common.WorkItems
{
    /// <summary>Unfinished planned work in a project, with the predecessors each of those work items waits on.</summary>
    public sealed record PlannedWorkSet(
        IReadOnlyList<PlannedWorkItemRecord> WorkItems,
        IReadOnlyList<WorkItemPredecessorRecord> Predecessors)
    {
        public static PlannedWorkSet Empty { get; } = new([], []);
    }

    /// <param name="ExpectedFinishAt">WorkItems.ExpectedFinishAt, the plan's expected finish; never an acknowledged completion time.</param>
    public sealed record PlannedWorkItemRecord(
        int WorkItemId,
        string Title,
        string SegmentName,
        DateTimeOffset PlannedStartAt,
        DateTimeOffset? ExpectedFinishAt);

    /// <summary>One WorkItemDependencies row: the predecessor must hand over before the successor can start.</summary>
    /// <param name="PredecessorExpectedFinishAt">Carried here so a predecessor outside the planned set still supplies its expected finish.</param>
    public sealed record WorkItemPredecessorRecord(
        int SuccessorWorkItemId,
        int PredecessorWorkItemId,
        string PredecessorTitle,
        DateTimeOffset? PredecessorExpectedFinishAt);
}
