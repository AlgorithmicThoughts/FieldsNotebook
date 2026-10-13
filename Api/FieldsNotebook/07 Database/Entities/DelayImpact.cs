namespace Database.Entities;

/// <summary>Maps dbo.DelayImpacts. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class DelayImpact
{
    public int Id { get; set; }
    public int DelayId { get; set; }
    public int ProjectId { get; set; }
    public int BlockedCrewId { get; set; }
    public int? BlockedWorkItemId { get; set; }
}
