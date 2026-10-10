namespace Database.Entities
{
    /// <summary>Maps dbo.SiteMaps. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class SiteMap
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
