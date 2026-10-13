namespace DataContract.Upcoming;

/// <summary>
/// GET /api/projects/{projectId}/upcoming — the unresolved blockers on work that should be under way by
/// the end of the next project day, one row per affected crew and work item.
/// </summary>
public sealed record UpcomingResponse(
    IReadOnlyList<BlockedWorkResponse> BlockedWork);
