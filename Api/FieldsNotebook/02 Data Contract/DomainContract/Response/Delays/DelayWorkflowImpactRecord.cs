namespace DataContract.Domain.Delays;

/// <param name="IsVerifiedInCycle">The affected crew's Foreman already verified this impact in the latest cycle.</param>
public sealed record DelayWorkflowImpactRecord(
    int ImpactId,
    int BlockedCrewId,
    bool IsVerifiedInCycle);
