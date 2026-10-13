namespace DataContract.Me;

/// <summary>GET /api/me — the signed-in user, their settings, and every active project assignment.</summary>
/// <param name="ThemePreference">UserSettings.ThemePreference: System, Light, or Dark.</param>
public sealed record CurrentUserResponse(
    int Id,
    string DisplayName,
    string Email,
    string? Phone,
    string ThemePreference,
    bool EmailEnabled,
    bool SmsEnabled,
    IReadOnlyList<CurrentUserProjectResponse> Projects);
