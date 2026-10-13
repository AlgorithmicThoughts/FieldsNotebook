using Common.Users;

namespace DataContract.Domain.Users;

/// <summary>Settings for a user with no UserSettings row: System appearance, email on, SMS off.</summary>
public static class UserSettingsDefaults
{
    public static UserSettingsRecord Value { get; } = new(ThemePreferences.System, EmailEnabled: true, SmsEnabled: false);
}
