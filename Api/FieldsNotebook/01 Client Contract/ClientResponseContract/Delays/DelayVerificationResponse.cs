using DataContract.Home;

namespace DataContract.Delays;

public sealed record DelayVerificationResponse(
    string VerifiedByName,
    DateTimeOffset VerifiedAt,
    string? Note);
