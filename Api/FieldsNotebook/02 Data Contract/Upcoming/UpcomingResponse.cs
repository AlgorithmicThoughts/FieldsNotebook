namespace DataContract.Upcoming
{
    /// <summary>
    /// GET /api/projects/{projectId}/upcoming — the unresolved blockers on work that should be under way by
    /// the end of the next project day, one row per affected crew and work item.
    /// </summary>
    public sealed record UpcomingResponse(IReadOnlyList<BlockedWorkResponse> BlockedWork);

    /// <summary>One blocked work item. One delay with several impacts produces several rows.</summary>
    /// <param name="Id">DelayImpacts.Id, which is unique per affected crew and work item.</param>
    /// <param name="BlockedCrewName">The crew that cannot start, from DelayImpacts.BlockedCrewId.</param>
    /// <param name="OwnerCrewName">The crew that must remove the blocker.</param>
    /// <param name="PlannedStartAt">WorkItems.PlannedStartAt in UTC.</param>
    public sealed record BlockedWorkResponse(
        int Id,
        string WorkItemTitle,
        string SegmentName,
        string BlockedCrewName,
        DateTimeOffset PlannedStartAt,
        string DelayNumber,
        string DelayTitle,
        string OwnerCrewName,
        string TagName,
        bool IsAdvanceNotice,
        bool IsHighPriority,
        CompletionEstimateResponse Completion,
        BlockedPredecessorResponse? BlockedPredecessor);

    /// <summary>
    /// The tentative completion the row shows, and which field it was read from. The two sources mean
    /// different things, so the row says which one it is. Neither clears the blocker.
    /// </summary>
    /// <param name="Source">A value of <see cref="CompletionEstimateSources"/>.</param>
    /// <param name="WorkItemTitle">The predecessor the estimate came from, for the workItem source only.</param>
    public sealed record CompletionEstimateResponse(string Source, DateTimeOffset? At, string? WorkItemTitle);

    public static class CompletionEstimateSources
    {
        /// <summary>Nobody has given a tentative completion time.</summary>
        public const string None = "none";

        /// <summary>DelayCycles.ExpectedReadyAt for the delay's active cycle.</summary>
        public const string DelayCycle = "delayCycle";

        /// <summary>WorkItems.ExpectedFinishAt on a predecessor, which nobody acknowledged.</summary>
        public const string WorkItem = "workItem";
    }

    /// <summary>A predecessor that an unresolved delay still sits on.</summary>
    public sealed record BlockedPredecessorResponse(string Title, string DelayNumber);
}
