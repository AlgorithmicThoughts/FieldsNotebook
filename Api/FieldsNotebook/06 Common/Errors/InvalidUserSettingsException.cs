namespace Common.Errors
{
    /// <summary>The settings change is empty or names a preference Field Notebook does not offer.</summary>
    public sealed class InvalidUserSettingsException(string message) : Exception(message);
}
