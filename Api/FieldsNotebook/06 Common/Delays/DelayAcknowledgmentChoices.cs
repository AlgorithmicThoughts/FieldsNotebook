namespace Common.Delays
{
    /// <summary>
    /// DelayCycles.AcknowledgmentChoice values the clients offer. The column is an open string, so an
    /// unrecognised answer is still stored; only these two carry workflow meaning.
    /// </summary>
    public static class DelayAcknowledgmentChoices
    {
        public const string WorkRemains = "WorkRemains";
        public const string ReadyForHandover = "ReadyForHandover";

        /// <summary>The answer is that the work is already ready, so nothing remains before the handover.</summary>
        public static bool IsReadyForHandover(string choice) =>
            string.Equals(choice, ReadyForHandover, StringComparison.OrdinalIgnoreCase);
    }
}
