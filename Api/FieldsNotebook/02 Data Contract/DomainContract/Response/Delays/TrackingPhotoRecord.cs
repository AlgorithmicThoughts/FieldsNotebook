namespace DataContract.Domain.Delays;

public sealed record TrackingPhotoRecord(
    int Id,
    int DelayId,
    string Purpose,
    string OriginalUrl,
    string ThumbnailUrl,
    string FileName,
    DateTimeOffset CreatedAt,
    decimal? Latitude,
    decimal? Longitude);
