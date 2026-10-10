namespace DataContract.Me
{
    /// <summary>The user's saved UserSettings after a change.</summary>
    /// <param name="ThemePreference">System, Light, or Dark.</param>
    public sealed record UserSettingsResponse(string ThemePreference, bool EmailEnabled, bool SmsEnabled);
}
