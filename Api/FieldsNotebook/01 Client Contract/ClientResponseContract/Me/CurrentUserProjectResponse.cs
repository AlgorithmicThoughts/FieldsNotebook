namespace DataContract.Me;

/// <summary>One accessible project with the user's role and the crews that role covers.</summary>
public sealed record CurrentUserProjectResponse(
    int ProjectId,
    string Name,
    string Code,
    string DistrictName,
    string RoleName,
    IReadOnlyList<CrewResponse> Crews,
    bool CanReportDelay);
