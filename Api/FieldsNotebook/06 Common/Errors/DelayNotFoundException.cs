namespace Common.Errors
{
    /// <summary>The delay does not exist in the project the request names.</summary>
    public sealed class DelayNotFoundException(int delayId)
        : Exception($"Delay {delayId} is not available in this project.")
    {
        public int DelayId { get; } = delayId;
    }
}
