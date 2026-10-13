namespace DataContract.Domain.Projects;

/// <summary>An active crew's Foreman lead or Superintendent who holds active access to the crew's project.</summary>
public sealed record CrewContactRecord(
    int CrewId,
    int UserId);
