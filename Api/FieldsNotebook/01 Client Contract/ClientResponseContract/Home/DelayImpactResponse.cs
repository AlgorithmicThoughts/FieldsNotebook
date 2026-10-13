namespace DataContract.Home;

/// <param name="WorkItemId">DelayImpacts.BlockedWorkItemId, or null when no work item was named.</param>
public sealed record DelayImpactResponse(
    int Id,
    string CrewName,
    int? WorkItemId);
