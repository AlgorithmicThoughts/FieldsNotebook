namespace DataContract.Domain.Delays;

public sealed record ReportPhotoRecord(
    string OriginalUrl,
    string ThumbnailUrl,
    string FileName,
    decimal? Latitude,
    decimal? Longitude);
