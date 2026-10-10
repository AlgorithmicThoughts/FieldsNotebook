namespace Common.Users
{
    /// <summary>The UserSettings.ThemePreference values, in the casing the table stores.</summary>
    public static class ThemePreferences
    {
        public const string System = "System";
        public const string Light = "Light";
        public const string Dark = "Dark";

        public static bool IsKnown(string value) => value is System or Light or Dark;
    }
}
