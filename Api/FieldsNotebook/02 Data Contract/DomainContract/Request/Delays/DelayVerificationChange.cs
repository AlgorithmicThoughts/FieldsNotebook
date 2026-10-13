namespace DataContract.Domain.Delays;

/// <summary>One successful handover check to store against an impact and the cycle that requires it.</summary>
public sealed record DelayVerificationChange(
    int ImpactId,
    string? Note);
