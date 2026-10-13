namespace Database.Entities;

/// <summary>Maps dbo.Segments. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class Segment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
