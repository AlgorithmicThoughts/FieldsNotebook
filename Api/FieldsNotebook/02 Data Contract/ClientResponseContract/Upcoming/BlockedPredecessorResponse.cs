namespace DataContract.Upcoming;

/// <summary>A predecessor that an unresolved delay still sits on.</summary>
public sealed record BlockedPredecessorResponse(
    string Title,
    string DelayNumber);
