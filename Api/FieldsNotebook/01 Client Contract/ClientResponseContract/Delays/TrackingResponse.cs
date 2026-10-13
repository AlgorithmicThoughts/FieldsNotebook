using DataContract.Home;

namespace DataContract.Delays;

public sealed record TrackingResponse(
    int ProjectId,
    string ProjectName,
    IReadOnlyList<TrackingDelayResponse> Delays);
