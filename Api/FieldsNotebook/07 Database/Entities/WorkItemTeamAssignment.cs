namespace Database.Entities;

/// <summary>Maps dbo.WorkItemTeamAssignment. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class WorkItemTeamAssignment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int WorkItemId { get; set; }
    public int CrewId { get; set; }
}
