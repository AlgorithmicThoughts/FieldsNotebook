namespace DataContract.Domain.Users;

/// <summary>An active crew the user leads as its Foreman or directs as its Superintendent.</summary>
public sealed record CrewLeadRecord(
    int CrewId,
    int ProjectId,
    string Name,
    bool IsForemanLead);
