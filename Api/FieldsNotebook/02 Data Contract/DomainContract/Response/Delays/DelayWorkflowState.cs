namespace DataContract.Domain.Delays;

/// <summary>
/// Everything one workflow action validates against, read in a single pass before the write.
/// The cycle described here is the delay's latest cycle, open or closed.
/// </summary>
/// <param name="CycleCount">All cycles of the delay; more than one means the delay was reopened.</param>
public sealed record DelayWorkflowState(
    int DelayId,
    int ProjectId,
    string StatusCode,
    int StatusId,
    int OwnerCrewId,
    int ReportedByCrewId,
    int CycleId,
    bool IsCycleOpen,
    bool IsCycleAcknowledged,
    int CycleCount,
    DateTimeOffset? ExpectedReadyAt,
    IReadOnlyList<DelayWorkflowImpactRecord> Impacts);
