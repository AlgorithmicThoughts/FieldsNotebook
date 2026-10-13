namespace Database.Entities;

/// <summary>Maps dbo.DelayImpactVerifications. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class DelayImpactVerification
{
    public int DelayImpactId { get; set; }
    public int DelayId { get; set; }
    public int DelayCycleId { get; set; }
    public int VerifiedByUserId { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public string? VerificationNote { get; set; }
}
