namespace Common.Users
{
    /// <param name="Settings">UserSettings for the user, or null when no row exists.</param>
    public sealed record CurrentUserRecord(
        int Id,
        string DisplayName,
        string Email,
        string? Phone,
        UserSettingsRecord? Settings,
        IReadOnlyList<UserProjectRecord> Projects,
        IReadOnlyList<CrewLeadRecord> Crews);

    public sealed record UserSettingsRecord(string ThemePreference, bool EmailEnabled, bool SmsEnabled);

    /// <summary>An active project assignment with the user's role in that project.</summary>
    public sealed record UserProjectRecord(int ProjectId, string Name, string Code, string DistrictName, string RoleName);

    /// <summary>An active crew the user leads as its Foreman or directs as its Superintendent.</summary>
    public sealed record CrewLeadRecord(int CrewId, int ProjectId, string Name, bool IsForemanLead);
}
