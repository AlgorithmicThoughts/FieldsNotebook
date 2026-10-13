using Common.Errors;
using DataContract.Delays;

namespace Engines.Delays;

/// <summary>The upload rules every delay photo meets, whether it reports a delay or proves the work.</summary>
internal static class DelayPhotoRules
{
    public const int MaxPhotos = 5;

    private const long MaxOriginalBytes = 25 * 1024 * 1024;
    private const long MaxThumbnailBytes = 2 * 1024 * 1024;

    /// <summary>The first rule the photos break, or null when every photo is acceptable.</summary>
    public static RequestError? Validate(IReadOnlyCollection<DelayReportPhoto> photos, string countMessage)
    {
        if (photos.Count is < 1 or > MaxPhotos)
        {
            return RequestError.InvalidRequest(countMessage);
        }

        return photos.Select(ValidatePhoto).FirstOrDefault(error => error is not null);
    }

    #region Private Methods

    // Coordinate ranges and the latitude/longitude pair are checked on the request contract.
    private static RequestError? ValidatePhoto(DelayReportPhoto photo)
    {
        if (photo.File is not { Length: > 0 and <= MaxOriginalBytes })
        {
            return RequestError.InvalidRequest("Each photo must be nonempty and no larger than 25 MB.");
        }

        if (photo.File.ContentType is not ("image/jpeg" or "image/png" or "image/heic" or "image/heif"))
        {
            return RequestError.InvalidRequest("Use JPG, PNG, or HEIC photos.");
        }

        if (photo.Thumbnail is not { Length: > 0 and <= MaxThumbnailBytes, ContentType: "image/jpeg" })
        {
            return RequestError.InvalidRequest("A JPEG thumbnail is required for each photo.");
        }

        if (Path.GetFileName(photo.File.FileName).Length > 255)
        {
            return RequestError.InvalidRequest("Photo file names allow up to 255 characters.");
        }

        return null;
    }

    #endregion
}
