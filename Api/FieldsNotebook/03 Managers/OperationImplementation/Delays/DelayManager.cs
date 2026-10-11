using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using DataContract.Delays;
using DataContract.Home;
using Engines.Delays;
using Utilities.OperationContract;
using Utilities;

namespace Managers.Delays;

public partial class DelayManager(AccessorFactory accessorFactory, UtilityFactory? Utilities = null) : IDelayManager
{
    public async Task<TrackingResponse> GetProjectTrackingDetailsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
        var access = await accessorFactory.CreateAccessor<IProjectAccessAccessor>()
            .GetProjectAccessAsync(userId, projectId, cancellationToken)
            ?? throw new ProjectAccessDeniedException(projectId);
        var data = await accessorFactory.CreateAccessor<IDelayAccessor>().GetProjectTrackingDetailsAsync(projectId, cancellationToken);
        var mapping = new TrackingMapping(data, projectId, access.ProjectName,
            Utilities?.CreateManager<IAzureBlobStorageUtility>());
        return new TrackingResponse(projectId, access.ProjectName, data.Delays.Select(mapping.ToDelay).ToList());
    }

    private sealed class TrackingMapping
    {
        private readonly int _projectId;
        private readonly IAzureBlobStorageUtility? _storage;
        private readonly string _projectName;
        private readonly ILookup<int, TrackingImpactRecord> _impacts;
        private readonly ILookup<int, TrackingCycleRecord> _cycles;
        private readonly ILookup<int, TrackingPhotoRecord> _photos;
        private readonly ILookup<int, TrackingEventRecord> _events;
        private readonly ILookup<(int, int), TrackingVerificationRecord> _verifications;
        private readonly IReadOnlyDictionary<int, TrackingSiteMapRecord> _maps;

        public TrackingMapping(TrackingSet data, int projectId, string projectName, IAzureBlobStorageUtility? storage)
        {
            _projectId = projectId;
            _storage = storage;
            _projectName = projectName;
            _impacts = data.Impacts.ToLookup(i => i.DelayId);
            _cycles = data.Cycles.ToLookup(c => c.DelayId);
            _photos = data.Photos.ToLookup(p => p.DelayId);
            _events = data.Events.ToLookup(e => e.DelayId);
            _verifications = data.Verifications.ToLookup(v => (v.ImpactId, v.CycleId));
            _maps = data.SiteMaps.ToDictionary(m => m.Id);
        }

        public TrackingDelayResponse ToDelay(TrackingDelayRecord row)
        {
            var d = row.Summary;
            var cycles = _cycles[d.DelayId].Select((c, index) => new DelayCycleResponse(c.Id, index + 1, c.CreatedAt,
                c.OpenReason, c.AcknowledgmentChoice, c.AcknowledgmentNote, c.AcknowledgedAt, c.AcknowledgedByName,
                c.ExpectedReadyAt, c.ClosedAt)).ToList();
            var sequence = cycles.ToDictionary(c => c.Id, c => c.Sequence);
            var currentCycleId = cycles.LastOrDefault()?.Id;
            return new TrackingDelayResponse(ToSummary(d), d.Description,
                new DelayReporterResponse(row.ReporterId, row.ReporterName, row.ReporterCrewName, d.CreatedAt),
                cycles, ToPhotos(d.DelayId), ToImpacts(d.DelayId, currentCycleId),
                _events[d.DelayId].Select(e => new DelayEventResponse(e.Id, e.EventType, e.ActorName, e.CreatedAt,
                    sequence.TryGetValue(e.CycleId, out var number) ? number : null, e.OldExpectedReadyAt, e.NewExpectedReadyAt)).ToList(),
                ToSiteMap(d.SiteMapId));
        }

        private DelaySummaryResponse ToSummary(ActiveDelayRecord d) =>
            new(d.DelayId, d.Number, d.Title, d.StatusCode, d.TagCode, d.TagName, d.OwnerCrewName,
                _impacts[d.DelayId].Select(i => i.CrewName).Distinct().ToList(),
                _impacts[d.DelayId].Select(i => new DelayImpactResponse(i.Id, i.CrewName, i.WorkItemId)).ToList(),
                d.LocationName, _projectId, _projectName, DelayRulesEngine.IsHighPriority(d.CycleCount, d.HasOpenCycle),
                d.ReportType, d.CreatedAt, d.ExpectedReadyAt,
                d.PinX is { } x && d.PinY is { } y ? new DelayPinResponse(x, y) : null, d.SiteMapId, d.SegmentName);

        private IReadOnlyList<TrackingImpactResponse> ToImpacts(int delayId, int? cycleId) =>
            _impacts[delayId].Select(i => new TrackingImpactResponse(i.Id, i.CrewName, i.WorkItemId, i.WorkItemTitle,
                _verifications[(i.Id, cycleId ?? 0)].Select(v => new DelayVerificationResponse(v.VerifiedByName, v.VerifiedAt, v.Note)).ToList())).ToList();

        private IReadOnlyList<DelayPhotoGroupResponse> ToPhotos(int delayId) =>
            _photos[delayId].GroupBy(p => p.Purpose).Select(g => new DelayPhotoGroupResponse(g.Key,
                g.Select(p => new DelayPhotoResponse(p.Id, ReadUrl(p.OriginalUrl), ReadUrl(p.ThumbnailUrl), p.FileName, p.CreatedAt, p.Latitude, p.Longitude)).ToList())).ToList();

        private string ReadUrl(string url) => _storage?.GetReadUrl(url) ?? url;

        private TrackingSiteMapResponse? ToSiteMap(int? id) =>
            id is { } value && _maps.TryGetValue(value, out var map) ? new(map.Id, map.Name, map.Uri) : null;
    }
}
