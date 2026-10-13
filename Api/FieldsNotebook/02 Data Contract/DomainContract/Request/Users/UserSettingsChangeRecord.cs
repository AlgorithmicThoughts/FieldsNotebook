namespace DataContract.Domain.Users;

/// <summary>A validated settings change. A null field keeps the saved value, or the default when no row exists.</summary>
public sealed record UserSettingsChangeRecord(
    string? ThemePreference,
    bool? EmailEnabled,
    bool? SmsEnabled);
