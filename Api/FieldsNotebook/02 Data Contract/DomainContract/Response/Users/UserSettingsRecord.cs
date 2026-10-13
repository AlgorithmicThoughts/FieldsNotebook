namespace DataContract.Domain.Users;

public sealed record UserSettingsRecord(
    string ThemePreference,
    bool EmailEnabled,
    bool SmsEnabled);
