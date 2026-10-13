namespace DataContract.Upcoming;

public static class CompletionEstimateSources
{
    /// <summary>Nobody has given a tentative completion time.</summary>
    public const string None = "none";

    /// <summary>DelayCycles.ExpectedReadyAt for the delay's active cycle.</summary>
    public const string DelayCycle = "delayCycle";

    /// <summary>WorkItems.ExpectedFinishAt on a predecessor, which nobody acknowledged.</summary>
    public const string WorkItem = "workItem";
}
