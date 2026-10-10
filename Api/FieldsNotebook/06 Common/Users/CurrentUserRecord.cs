namespace Common.Users
{
    public sealed record CurrentUserRecord(
        int Id,
        string DisplayName,
        IReadOnlyList<UserProjectRecord> Projects,
        IReadOnlyList<CrewLeadRecord> Crews);

    /// <summary>An active project assignment with the user's role in that project.</summary>
    public sealed record UserProjectRecord(int ProjectId, string Name, string Code, string DistrictName, string RoleName);

    /// <summary>An active crew the user leads as its Foreman or directs as its Superintendent.</summary>
    public sealed record CrewLeadRecord(int CrewId, int ProjectId, string Name, bool IsForemanLead);
}
