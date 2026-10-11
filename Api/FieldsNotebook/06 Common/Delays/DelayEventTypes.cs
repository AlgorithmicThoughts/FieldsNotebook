namespace Common.Delays
{
    /// <summary>ActivityEvents.EventType values the delay workflow appends.</summary>
    public static class DelayEventTypes
    {
        public const string Acknowledged = "delay.acknowledged";
        public const string ExpectedReadyChanged = "delay.expectedReadyChanged";
        public const string ReadyForHandover = "delay.readyForHandover";
        public const string HandoverVerified = "delay.handoverVerified";
        public const string Resolved = "delay.resolved";
        public const string Reopened = "delay.reopened";
        public const string Revoked = "delay.revoked";
    }
}
