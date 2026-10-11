namespace Common.Errors
{
    /// <summary>The user's role and crew do not carry authority for this delay action.</summary>
    public sealed class DelayActionNotPermittedException(int delayId, string action)
        : Exception($"The user cannot {action} delay {delayId}.")
    {
        public int DelayId { get; } = delayId;

        public string Action { get; } = action;
    }
}
