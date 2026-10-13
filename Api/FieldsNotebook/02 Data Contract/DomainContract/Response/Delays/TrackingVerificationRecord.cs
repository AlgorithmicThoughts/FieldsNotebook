namespace DataContract.Domain.Delays;

public sealed record TrackingVerificationRecord(
    int ImpactId,
    int CycleId,
    string VerifiedByName,
    DateTimeOffset VerifiedAt,
    string? Note);
