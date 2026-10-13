using DataContract.Domain.Delays;

namespace DataContract.Domain.Insights;

/// <summary>
/// How one delay's history reads on the tracking screen: cycles numbered from one, only the current
/// cycle's handover checks against each crew, photos grouped by purpose, and the priority rule.
/// </summary>
public sealed record TrackingDelayView(
    TrackingDelayRecord Delay,
    bool IsHighPriority,
    IReadOnlyList<TrackingCycleView> Cycles,
    IReadOnlyList<TrackingPhotoGroupView> Photos,
    IReadOnlyList<TrackingImpactView> Impacts,
    IReadOnlyList<TrackingEventView> Events,
    TrackingSiteMapRecord? SiteMap);

public sealed record TrackingCycleView(
    TrackingCycleRecord Cycle,
    int Sequence);

/// <param name="CycleSequence">The sequence of the event's cycle, or null when the cycle is not in the set.</param>
public sealed record TrackingEventView(
    TrackingEventRecord Event,
    int? CycleSequence);

/// <param name="CurrentCycleVerifications">Only the current cycle's checks: a reopen asks every affected crew to verify again.</param>
public sealed record TrackingImpactView(
    TrackingImpactRecord Impact,
    IReadOnlyList<TrackingVerificationRecord> CurrentCycleVerifications);

public sealed record TrackingPhotoGroupView(
    string Purpose,
    IReadOnlyList<TrackingPhotoRecord> Photos);
