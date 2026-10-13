namespace DataContract.Domain.Delays;

public sealed record TrackingSet(
    IReadOnlyList<TrackingDelayRecord> Delays,
    IReadOnlyList<TrackingImpactRecord> Impacts,
    IReadOnlyList<TrackingCycleRecord> Cycles,
    IReadOnlyList<TrackingVerificationRecord> Verifications,
    IReadOnlyList<TrackingPhotoRecord> Photos,
    IReadOnlyList<TrackingEventRecord> Events,
    IReadOnlyList<TrackingSiteMapRecord> SiteMaps)
{
    public static TrackingSet Empty { get; } = new([], [], [], [], [], [], []);
}
