namespace DataContract.Me
{
    public sealed record CurrentUserResponse(int Id, string DisplayName, IReadOnlyList<CurrentUserProjectResponse> Projects);

    /// <summary>One accessible project with the user's role and the crews that role covers.</summary>
    public sealed record CurrentUserProjectResponse(
        int ProjectId,
        string Name,
        string Code,
        string DistrictName,
        string RoleName,
        IReadOnlyList<string> CrewNames,
        bool CanReportDelay);
}
