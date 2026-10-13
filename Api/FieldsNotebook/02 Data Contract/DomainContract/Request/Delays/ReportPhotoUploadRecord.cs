namespace DataContract.Domain.Delays;

/// <summary>
/// One uploaded photo and its thumbnail as request-scoped stream sources, with the metadata the photo
/// rules check. Keeps HTTP upload types out of Engines and Accessors.
/// </summary>
/// <param name="FileName">The file name only, never a client path.</param>
public sealed record ReportPhotoUploadRecord(
    Func<Stream> OpenOriginal,
    Func<Stream> OpenThumbnail,
    string ContentType,
    string FileName,
    decimal? Latitude,
    decimal? Longitude,
    long Length,
    string ThumbnailContentType,
    long ThumbnailLength);
