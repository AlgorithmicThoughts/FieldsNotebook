namespace Common.Delays;

/// <summary>
/// DelayCycles.AcknowledgmentChoice values the clients offer. The column is an open string, so an
/// unrecognised answer is still stored. Neither answer moves the delay on: review always needs its own
/// submission with proof of work.
/// </summary>
public static class DelayAcknowledgmentChoices
{
    public const string WorkRemains = "WorkRemains";
    public const string ReadyForHandover = "ReadyForHandover";
}
