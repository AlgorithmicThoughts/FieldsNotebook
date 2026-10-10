namespace Database.Entities
{
    /// <summary>Maps dbo.Users. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class User
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
    }
}
