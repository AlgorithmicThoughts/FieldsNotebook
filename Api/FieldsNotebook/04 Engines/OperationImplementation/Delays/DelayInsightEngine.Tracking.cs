using DataContract.Domain.Delays;
using DataContract.Domain.Insights;

namespace Engines.Delays;

/// <summary>
/// How a delay's history reads on the tracking screen: cycles numbered from one, only the current
/// cycle's handover checks shown against each crew, photos grouped by purpose, and the priority rule.
/// </summary>
public sealed partial class DelayInsightEngine
{
    public IReadOnlyList<TrackingDelayView> BuildTracking(TrackingSet data)
    {
        var index = new TrackingIndex(data);
        return data.Delays.Select(row => BuildTrackingDelay(row, index)).ToList();
    }

    #region Private Methods

    private static TrackingDelayView BuildTrackingDelay(TrackingDelayRecord row, TrackingIndex index)
    {
        var delay = row.Summary;
        var cycles = index.Cycles[delay.DelayId].Select((c, i) => new TrackingCycleView(c, Sequence: i + 1)).ToList();
        var sequence = cycles.ToDictionary(c => c.Cycle.Id, c => c.Sequence);
        var currentCycleId = cycles.LastOrDefault()?.Cycle.Id;

        return new TrackingDelayView(row, IsHighPriority(delay.CycleCount, delay.HasOpenCycle), cycles,
            BuildPhotos(delay.DelayId, index), BuildImpacts(delay.DelayId, currentCycleId, index),
            index.Events[delay.DelayId].Select(e => new TrackingEventView(e, sequence.TryGetValue(e.CycleId, out var number) ? number : null)).ToList(),
            delay.SiteMapId is { } mapId && index.Maps.TryGetValue(mapId, out var map) ? map : null);
    }

    // Only the current cycle's checks count: a reopen asks every affected crew to verify again.
    private static IReadOnlyList<TrackingImpactView> BuildImpacts(int delayId, int? currentCycleId, TrackingIndex index) =>
        index.Impacts[delayId].Select(i => new TrackingImpactView(i, index.Verifications[(i.Id, currentCycleId ?? 0)].ToList())).ToList();

    private static IReadOnlyList<TrackingPhotoGroupView> BuildPhotos(int delayId, TrackingIndex index) =>
        index.Photos[delayId].GroupBy(p => p.Purpose).Select(g => new TrackingPhotoGroupView(g.Key, g.ToList())).ToList();

    #endregion

    /// <summary>The set's rows keyed by delay, built once so each delay maps without rescanning the set.</summary>
    private sealed class TrackingIndex(TrackingSet data)
    {
        public ILookup<int, TrackingImpactRecord> Impacts { get; } = data.Impacts.ToLookup(i => i.DelayId);

        public ILookup<int, TrackingCycleRecord> Cycles { get; } = data.Cycles.ToLookup(c => c.DelayId);

        public ILookup<int, TrackingPhotoRecord> Photos { get; } = data.Photos.ToLookup(p => p.DelayId);

        public ILookup<int, TrackingEventRecord> Events { get; } = data.Events.ToLookup(e => e.DelayId);

        public ILookup<(int ImpactId, int CycleId), TrackingVerificationRecord> Verifications { get; } =
            data.Verifications.ToLookup(v => (v.ImpactId, v.CycleId));

        public IReadOnlyDictionary<int, TrackingSiteMapRecord> Maps { get; } = data.SiteMaps.ToDictionary(m => m.Id);
    }
}
