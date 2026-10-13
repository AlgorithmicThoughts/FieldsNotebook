using DataContract.Domain.Users;

namespace DataContract.Domain.Projects;

/// <summary>The user's active access to one project: their role there and the crews they lead or direct.</summary>
public sealed record ProjectAccessRecord(
    int ProjectId,
    string ProjectName,
    string RoleName,
    IReadOnlyList<CrewLeadRecord> Crews);
