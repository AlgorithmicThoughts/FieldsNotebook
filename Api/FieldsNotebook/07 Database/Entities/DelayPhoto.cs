namespace Database.Entities;

/// <summary>Maps dbo.DelayPhotos. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class DelayPhoto
{
    public int Id { get; set; }
    public int DelayId { get; set; }
    public int DelayCycleId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
