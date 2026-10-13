namespace DataContract.Domain.Delays;

/// <summary>Request-scoped stream sources; keeps HTTP upload types out of Accessors.</summary>
public sealed record ReportPhotoUploadRecord(
    Func<Stream> OpenOriginal,
    Func<Stream> OpenThumbnail,
    string ContentType,
    string FileName,
    decimal? Latitude,
    decimal? Longitude);
