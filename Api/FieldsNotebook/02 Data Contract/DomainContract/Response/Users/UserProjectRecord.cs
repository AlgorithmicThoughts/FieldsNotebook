namespace DataContract.Domain.Users;

/// <summary>An active project assignment with the user's role in that project.</summary>
public sealed record UserProjectRecord(
    int ProjectId,
    string Name,
    string Code,
    string DistrictName,
    string RoleName);
