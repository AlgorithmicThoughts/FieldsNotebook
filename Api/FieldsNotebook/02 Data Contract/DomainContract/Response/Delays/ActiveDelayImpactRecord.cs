namespace DataContract.Domain.Delays;

/// <param name="IsVerifiedInOpenCycle">The affected crew's Foreman verified this impact in the open cycle.</param>
public sealed record ActiveDelayImpactRecord(
    int ImpactId,
    int DelayId,
    int BlockedCrewId,
    string BlockedCrewName,
    int? BlockedWorkItemId,
    bool IsVerifiedInOpenCycle);
