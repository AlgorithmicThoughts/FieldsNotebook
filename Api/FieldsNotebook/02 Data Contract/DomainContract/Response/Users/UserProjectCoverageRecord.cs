namespace DataContract.Domain.Users;

/// <summary>One of the user's projects with the crews their role covers there and whether they can report a delay.</summary>
public sealed record UserProjectCoverageRecord(
    UserProjectRecord Project,
    IReadOnlyList<CrewLeadRecord> CoveredCrews,
    bool CanReportDelay);
