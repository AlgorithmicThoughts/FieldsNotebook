namespace Database.Entities
{
    /// <summary>Maps dbo.Crew. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class Crew
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ForemanLeadId { get; set; }
        public int SuperintendentUserId { get; set; }
        public bool IsActive { get; set; }
    }
}
