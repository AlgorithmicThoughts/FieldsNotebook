namespace DataContract.Domain.Users;

/// <param name="Settings">UserSettings for the user, or null when no row exists.</param>
public sealed record CurrentUserRecord(
    int Id,
    string DisplayName,
    string Email,
    string? Phone,
    UserSettingsRecord? Settings,
    IReadOnlyList<UserProjectRecord> Projects,
    IReadOnlyList<CrewLeadRecord> Crews);
