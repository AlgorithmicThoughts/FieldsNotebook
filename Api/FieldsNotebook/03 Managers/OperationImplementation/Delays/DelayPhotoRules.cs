using Common.Delays;
using Common.Errors;
using DataContract.Delays;

namespace Managers.Delays;

/// <summary>The upload rules every delay photo meets, whether it reports a delay or proves the work.</summary>
internal static class DelayPhotoRules
{
    public const int MaxPhotos = 5;

    public static void Validate(IReadOnlyCollection<DelayReportPhoto> photos, string countMessage)
    {
        Require(photos.Count is >= 1 and <= MaxPhotos, countMessage);
        foreach (var photo in photos) ValidatePhoto(photo);
    }

    /// <summary>Request-scoped stream sources, so Accessors never see HTTP upload types.</summary>
    public static IReadOnlyList<ReportPhotoUploadRecord> ToUploads(IEnumerable<DelayReportPhoto> photos) =>
        photos.Select(p => new ReportPhotoUploadRecord(p.File.OpenReadStream, p.Thumbnail.OpenReadStream,
            p.File.ContentType, Path.GetFileName(p.File.FileName), p.Latitude, p.Longitude)).ToList();

    private static void ValidatePhoto(DelayReportPhoto photo)
    {
        Require(photo.File is { Length: > 0 and <= 25 * 1024 * 1024 }, "Each photo must be nonempty and no larger than 25 MB.");
        Require(photo.File.ContentType is "image/jpeg" or "image/png" or "image/heic" or "image/heif", "Use JPG, PNG, or HEIC photos.");
        Require(photo.Thumbnail is { Length: > 0 and <= 2 * 1024 * 1024, ContentType: "image/jpeg" }, "A JPEG thumbnail is required for each photo.");
        Require(photo.Latitude.HasValue == photo.Longitude.HasValue, "Photo GPS requires both latitude and longitude.");
        Require(photo.Latitude is null or (>= -90 and <= 90), "Photo latitude is invalid.");
        Require(photo.Longitude is null or (>= -180 and <= 180), "Photo longitude is invalid.");
        Require(Path.GetFileName(photo.File.FileName).Length <= 255, "Photo file names allow up to 255 characters.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new DelayReportValidationException(message);
    }
}
