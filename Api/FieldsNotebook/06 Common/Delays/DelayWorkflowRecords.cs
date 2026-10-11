namespace Common.Delays
{
    /// <summary>
    /// Everything one workflow action validates against, read in a single pass before the write.
    /// The cycle described here is the delay's latest cycle, open or closed.
    /// </summary>
    /// <param name="CycleCount">All cycles of the delay; more than one means the delay was reopened.</param>
    /// <param name="HasProofOfWorkPhoto">A DelayPhotos row with Purpose ProofOfWork exists for this cycle.</param>
    public sealed record DelayWorkflowState(
        int DelayId,
        int ProjectId,
        string StatusCode,
        int StatusId,
        int OwnerCrewId,
        int ReportedByCrewId,
        int CycleId,
        bool IsCycleOpen,
        bool IsCycleAcknowledged,
        int CycleCount,
        DateTimeOffset? ExpectedReadyAt,
        bool HasProofOfWorkPhoto,
        IReadOnlyList<DelayWorkflowImpactRecord> Impacts);

    /// <param name="IsVerifiedInCycle">The affected crew's Foreman already verified this impact in the latest cycle.</param>
    public sealed record DelayWorkflowImpactRecord(int ImpactId, int BlockedCrewId, bool IsVerifiedInCycle);

    /// <summary>
    /// The cycle half of one workflow write. A reopen opens a new cycle; every other action updates
    /// the delay's open cycle. A null <paramref name="ExpectedReadyAt"/> leaves the stored time alone,
    /// because no approved action clears a tentative completion time.
    /// </summary>
    public sealed record DelayCycleChange(
        int? CycleId,
        string? OpenReason,
        DelayAcknowledgmentChange? Acknowledgment,
        DateTimeOffset? ExpectedReadyAt,
        bool Close)
    {
        public static DelayCycleChange Open(string openReason) => new(null, openReason, null, null, false);

        public static DelayCycleChange Touch(int cycleId) => new(cycleId, null, null, null, false);

        public static DelayCycleChange Acknowledge(int cycleId, DelayAcknowledgmentChange acknowledgment, DateTimeOffset? expectedReadyAt) =>
            new(cycleId, null, acknowledgment, expectedReadyAt, false);

        public static DelayCycleChange ReviseExpectedReady(int cycleId, DateTimeOffset expectedReadyAt) =>
            new(cycleId, null, null, expectedReadyAt, false);

        public static DelayCycleChange CloseCycle(int cycleId) => new(cycleId, null, null, null, true);
    }

    /// <summary>The first acknowledgment of a cycle. A later action never overwrites it.</summary>
    public sealed record DelayAcknowledgmentChange(string Choice, string? Note);

    /// <summary>One successful handover check to store against an impact and the cycle that requires it.</summary>
    public sealed record DelayVerificationChange(int ImpactId, string? Note);

    /// <summary>
    /// One atomic delay workflow write: the status move, the cycle change, an optional handover check,
    /// the activity event that preserves the action, and one notice per recipient.
    /// </summary>
    /// <param name="ToStatusId">Null when the action changes no status, such as a revised completion time.</param>
    /// <param name="RecipientUserIds">Distinct recipients found from crew relationships, never including the actor.</param>
    public sealed record DelayWorkflowWrite(
        int DelayId,
        int ProjectId,
        int ActorUserId,
        DateTimeOffset OccurredAt,
        string EventType,
        int FromStatusId,
        int? ToStatusId,
        DelayCycleChange Cycle,
        DelayVerificationChange? Verification,
        DateTimeOffset? OldExpectedReadyAt,
        DateTimeOffset? NewExpectedReadyAt,
        IReadOnlyList<int> RecipientUserIds);

    /// <summary>What the committed workflow write produced.</summary>
    public sealed record DelayWorkflowResult(int CycleId, int ActivityEventId, int NoticeCount);
}
