using DataContract.Home;

namespace DataContract.Delays;

public sealed record DelayPhotoResponse(
    int Id,
    string OriginalUrl,
    string ThumbnailUrl,
    string FileName,
    DateTimeOffset CreatedAt,
    decimal? Latitude,
    decimal? Longitude);
