using DataContract.Home;

namespace DataContract.Delays;

public sealed record TrackingImpactResponse(
    int Id,
    string CrewName,
    int? WorkItemId,
    string? WorkItemTitle,
    IReadOnlyList<DelayVerificationResponse> Verifications);
