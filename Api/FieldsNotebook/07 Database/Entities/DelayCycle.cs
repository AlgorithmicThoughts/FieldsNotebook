namespace Database.Entities;

/// <summary>Maps dbo.DelayCycles. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class DelayCycle
{
    public int DelayCycleId { get; set; }
    public int DelayId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public int? ModifiedBy { get; set; }
    public string? OpenReason { get; set; }
    public string? AcknowledgmentChoice { get; set; }
    public int? AcknowledgedByUserId { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? AcknowledgmentNote { get; set; }
    public DateTimeOffset? ExpectedReadyAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
