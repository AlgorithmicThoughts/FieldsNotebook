namespace Database.Entities
{
    /// <summary>Maps dbo.Roles. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
