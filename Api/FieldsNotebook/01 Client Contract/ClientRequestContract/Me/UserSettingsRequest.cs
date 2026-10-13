using System.ComponentModel.DataAnnotations;
using Common.Users;

namespace DataContract.Me;

/// <summary>PATCH /api/me/settings — only the preferences the user changed; a null field keeps its saved value.</summary>
/// <param name="ThemePreference">System, Light, or Dark.</param>
public sealed record UserSettingsRequest(
    [AllowedValues(null, ThemePreferences.System, ThemePreferences.Light, ThemePreferences.Dark,
        ErrorMessage = "Appearance must be System, Light, or Dark.")] string? ThemePreference,
    bool? EmailEnabled,
    bool? SmsEnabled) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ThemePreference is null && EmailEnabled is null && SmsEnabled is null)
        {
            yield return new ValidationResult("Choose a setting to change.");
        }
    }
}
