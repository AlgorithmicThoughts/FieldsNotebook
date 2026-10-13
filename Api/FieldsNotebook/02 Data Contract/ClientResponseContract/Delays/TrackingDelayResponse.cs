using DataContract.Home;

namespace DataContract.Delays;

public sealed record TrackingDelayResponse(
    DelaySummaryResponse Summary,
    string Description,
    DelayReporterResponse ReportedBy,
    IReadOnlyList<DelayCycleResponse> Cycles,
    IReadOnlyList<DelayPhotoGroupResponse> Photos,
    IReadOnlyList<TrackingImpactResponse> Impacts,
    IReadOnlyList<DelayEventResponse> Events,
    TrackingSiteMapResponse? SiteMap);
