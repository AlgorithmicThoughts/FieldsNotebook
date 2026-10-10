namespace Database.Entities
{
    /// <summary>Maps dbo.UserSettings. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class UserSettings
    {
        public int UserId { get; set; }
        public string ThemePreference { get; set; } = string.Empty;
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
    }
}
