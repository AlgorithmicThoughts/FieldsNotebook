namespace Database.Entities;

/// <summary>Maps dbo.ActivityEvents. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class ActivityEvent
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int DelayId { get; set; }
    public int DelayCycleId { get; set; }
    public int? DelayImpactId { get; set; }
    public int ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int? FromDelayStatusId { get; set; }
    public int? ToDelayStatusId { get; set; }
    public DateTimeOffset? OldExpectedReadyAt { get; set; }
    public DateTimeOffset? NewExpectedReadyAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
