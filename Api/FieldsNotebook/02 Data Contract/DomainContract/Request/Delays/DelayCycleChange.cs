namespace DataContract.Domain.Delays;

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
