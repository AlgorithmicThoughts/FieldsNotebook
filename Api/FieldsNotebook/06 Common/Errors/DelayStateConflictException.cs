namespace Common.Errors
{
    /// <summary>The delay's current state does not permit this action, so nothing was written.</summary>
    public sealed class DelayStateConflictException(int delayId, string reason)
        : Exception($"Delay {delayId} cannot change: {reason}")
    {
        public int DelayId { get; } = delayId;

        public string Reason { get; } = reason;
    }
}
