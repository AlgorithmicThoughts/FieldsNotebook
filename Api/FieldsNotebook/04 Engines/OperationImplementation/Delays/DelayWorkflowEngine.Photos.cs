using Common.Errors;
using DataContract.Domain.Delays;

namespace Engines.Delays;

public sealed partial class DelayWorkflowEngine
{
    /// <summary>The upload rules every delay photo meets, whether it reports a delay or proves the work.</summary>
    private static class PhotoRules
    {
        private const int MaxPhotos = 5;
        private const long MaxOriginalBytes = 25 * 1024 * 1024;
        private const long MaxThumbnailBytes = 2 * 1024 * 1024;

        /// <summary>The first rule the photos break, or null when every photo is acceptable.</summary>
        public static RequestError? Validate(IReadOnlyCollection<ReportPhotoUploadRecord> photos, string countMessage)
        {
            if (photos.Count is < 1 or > MaxPhotos)
            {
                return RequestError.InvalidRequest(countMessage);
            }

            return photos.Select(ValidatePhoto).FirstOrDefault(error => error is not null);
        }

        // Coordinate ranges and the latitude/longitude pair are checked on the request contract.
        private static RequestError? ValidatePhoto(ReportPhotoUploadRecord photo)
        {
            if (photo.Length is <= 0 or > MaxOriginalBytes)
            {
                return RequestError.InvalidRequest("Each photo must be nonempty and no larger than 25 MB.");
            }

            if (photo.ContentType is not ("image/jpeg" or "image/png" or "image/heic" or "image/heif"))
            {
                return RequestError.InvalidRequest("Use JPG, PNG, or HEIC photos.");
            }

            if (photo is not { ThumbnailLength: > 0 and <= MaxThumbnailBytes, ThumbnailContentType: "image/jpeg" })
            {
                return RequestError.InvalidRequest("A JPEG thumbnail is required for each photo.");
            }

            if (photo.FileName.Length > 255)
            {
                return RequestError.InvalidRequest("Photo file names allow up to 255 characters.");
            }

            return null;
        }
    }
}
