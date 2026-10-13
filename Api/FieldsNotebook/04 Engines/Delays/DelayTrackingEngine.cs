using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
using DataContract.Mapping.DomainToClientMapping;

namespace Engines.Delays;

/// <summary>
/// How a delay's history reads on the tracking screen: cycles numbered from one, only the current
/// cycle's handover checks shown against each crew, photos grouped by purpose, and the priority rule.
/// </summary>
public static class DelayTrackingEngine
{
    /// <param name="readUrl">Turns a stored blob URL into one the client can open.</param>
    public static TrackingResponse BuildTracking(TrackingSet data, ProjectAccessRecord access, Func<string, string> readUrl) =>
        new(access.ProjectId, access.ProjectName, BuildDelays(data, access, readUrl));

    /// <param name="readUrl">Turns a stored blob URL into one the client can open.</param>
    public static IReadOnlyList<TrackingDelayResponse> BuildDelays(TrackingSet data, ProjectAccessRecord access, Func<string, string> readUrl)
    {
        var index = new TrackingIndex(data);
        return data.Delays.Select(row => BuildDelay(row, index, access, readUrl)).ToList();
    }

    #region Private Methods

    private static TrackingDelayResponse BuildDelay(TrackingDelayRecord row, TrackingIndex index, ProjectAccessRecord access,
        Func<string, string> readUrl)
    {
        var delay = row.Summary;
        var cycles = index.Cycles[delay.DelayId].Select((c, i) => c.ToResponse(sequence: i + 1)).ToList();
        var sequence = cycles.ToDictionary(c => c.Id, c => c.Sequence);
        var currentCycleId = cycles.LastOrDefault()?.Id;

        return new TrackingDelayResponse(BuildSummary(delay, index, access), delay.Description,
            new DelayReporterResponse(row.ReporterId, row.ReporterName, row.ReporterCrewName, delay.CreatedAt),
            cycles, BuildPhotos(delay.DelayId, index, readUrl), BuildImpacts(delay.DelayId, currentCycleId, index),
            index.Events[delay.DelayId].Select(e => e.ToResponse(sequence.TryGetValue(e.CycleId, out var number) ? number : null)).ToList(),
            delay.SiteMapId is { } mapId && index.Maps.TryGetValue(mapId, out var map) ? map.ToResponse() : null);
    }

    private static DelaySummaryResponse BuildSummary(ActiveDelayRecord delay, TrackingIndex index, ProjectAccessRecord access)
    {
        return delay.ToSummaryResponse(index.Impacts[delay.DelayId].Select(i => i.ToResponse()), access,
            DelayRulesEngine.IsHighPriority(delay.CycleCount, delay.HasOpenCycle));
    }

    // Only the current cycle's checks count: a reopen asks every affected crew to verify again.
    private static IReadOnlyList<TrackingImpactResponse> BuildImpacts(int delayId, int? currentCycleId, TrackingIndex index) =>
        index.Impacts[delayId].Select(i => new TrackingImpactResponse(i.Id, i.CrewName, i.WorkItemId, i.WorkItemTitle,
            index.Verifications[(i.Id, currentCycleId ?? 0)].Select(v => v.ToResponse()).ToList())).ToList();

    private static IReadOnlyList<DelayPhotoGroupResponse> BuildPhotos(int delayId, TrackingIndex index, Func<string, string> readUrl) =>
        index.Photos[delayId].GroupBy(p => p.Purpose)
            .Select(g => new DelayPhotoGroupResponse(g.Key, g.Select(p => p.ToResponse(readUrl)).ToList())).ToList();

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
