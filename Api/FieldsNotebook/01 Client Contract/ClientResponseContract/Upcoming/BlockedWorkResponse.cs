namespace DataContract.Upcoming;

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
