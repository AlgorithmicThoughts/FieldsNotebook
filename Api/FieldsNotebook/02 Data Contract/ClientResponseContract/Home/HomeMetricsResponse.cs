namespace DataContract.Home;

/// <summary>Counts across every active delay for the user's crews, not only the rows shown.</summary>
public sealed record HomeMetricsResponse(
    int BlockingOurCrews,
    int CrewsWaitingOnUs,
    int ReadyToVerify);
