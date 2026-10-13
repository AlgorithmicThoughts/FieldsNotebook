using Common.Delays;
using DataContract.Domain.Delays;
using Microsoft.Extensions.Logging;
using Utilities.OperationContract;

namespace Accessors.Delays;

/// <summary>
/// Uploads one write's delay photos and remembers every blob it attempted, so a failed
/// transaction can remove partial uploads instead of leaving orphaned files.
/// </summary>
internal sealed class DelayPhotoStorage(IAzureBlobStorageUtility storage, ILogger logger)
{
    private readonly List<string> _attempted = [];

    public async Task<IReadOnlyList<ReportPhotoRecord>> UploadAsync(IReadOnlyList<ReportPhotoUploadRecord> photos,
        string prefix, CancellationToken token)
    {
        var result = new List<ReportPhotoRecord>();
        foreach (var photo in photos)
            result.Add(await UploadPhotoAsync(photo, prefix, token));
        return result;
    }

    public async Task CleanUpAsync()
    {
        foreach (var name in _attempted)
        {
            try { await storage.DeleteAsync(name, CancellationToken.None); }
            catch (Exception error) { logger.LogWarning(error, "Could not clean up failed delay upload {BlobName}", name); }
        }
    }

    #region Private Methods

    private async Task<ReportPhotoRecord> UploadPhotoAsync(ReportPhotoUploadRecord photo, string prefix, CancellationToken token)
    {
        var id = Guid.NewGuid().ToString("N");
        var originalName = $"{prefix}/{id}/original";
        var thumbnailName = $"{prefix}/{id}/thumbnail.jpg";
        // Include partially successful uploads in failure cleanup.
        _attempted.Add(originalName);
        _attempted.Add(thumbnailName);
        await using var original = photo.OpenOriginal();
        var originalUrl = await storage.UploadAsync(originalName, original, photo.ContentType, token);
        await using var thumbnail = photo.OpenThumbnail();
        var thumbnailUrl = await storage.UploadAsync(thumbnailName, thumbnail, "image/jpeg", token);
        return new(originalUrl, thumbnailUrl, photo.FileName, photo.Latitude, photo.Longitude);
    }

    #endregion
}
