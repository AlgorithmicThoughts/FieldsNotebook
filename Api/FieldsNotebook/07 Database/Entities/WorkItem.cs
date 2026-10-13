namespace Database.Entities;

/// <summary>Maps dbo.WorkItems. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class WorkItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SegmentId { get; set; }
    public DateTimeOffset PlannedStartAt { get; set; }
    public DateTimeOffset PlannedFinishAt { get; set; }
    public DateTimeOffset? ExpectedFinishAt { get; set; }
    public bool IsCompleted { get; set; }
}
