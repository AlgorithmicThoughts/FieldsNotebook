namespace Database.Entities;

/// <summary>Maps dbo.Projects. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class Project
{
    public int Id { get; set; }
    public int DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
