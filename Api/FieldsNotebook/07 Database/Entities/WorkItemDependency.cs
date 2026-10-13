namespace Database.Entities;

/// <summary>Maps dbo.WorkItemDependencies. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class WorkItemDependency
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int PredecessorWorkItemId { get; set; }
    public int SuccessorWorkItemId { get; set; }
}
