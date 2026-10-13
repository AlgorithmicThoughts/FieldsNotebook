namespace Database.Entities;

/// <summary>Maps dbo.DelayStatus. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class DelayStatus
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
