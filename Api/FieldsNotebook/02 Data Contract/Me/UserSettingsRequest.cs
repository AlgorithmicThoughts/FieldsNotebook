namespace DataContract.Me
{
    /// <summary>PATCH /api/me/settings — only the preferences the user changed; a null field keeps its saved value.</summary>
    /// <param name="ThemePreference">System, Light, or Dark.</param>
    public sealed record UserSettingsRequest(string? ThemePreference, bool? EmailEnabled, bool? SmsEnabled);
}
