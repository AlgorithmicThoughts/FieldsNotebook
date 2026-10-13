using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DataContract.Domain.Delays;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accessors.Delays;

/// <summary>
/// Keeps delay photos in a private Azure Blob Storage container. Every upload of a batch is remembered,
/// so a failed batch, or a write that fails after it, can remove partial uploads instead of leaving orphaned files.
/// </summary>
public sealed class DelayPhotoAccessor(IOptions<AzureBlobStorageOptions> options, ILogger<DelayPhotoAccessor> logger) : IDelayPhotoAccessor
{
    private BlobContainerClient? _container;

    private BlobContainerClient Container => _container ??= new BlobServiceClient(options.Value.ConnectionString)
        .GetBlobContainerClient(options.Value.ContainerName);

    public Task<StoredPhotoSet> StoreReportPhotosAsync(int projectId, int? blockingWorkItemId, DateTimeOffset reportedAt,
        IReadOnlyList<ReportPhotoUploadRecord> photos, CancellationToken cancellationToken)
    {
        // The delay has no identity until its row commits, so each report's photos get an upload folder of their own.
        var workItem = blockingWorkItemId?.ToString() ?? "unassigned";
        var prefix = $"project-{projectId}/{reportedAt:yyyy-MM-dd}/work-item-{workItem}/report-upload-{Guid.NewGuid():N}/images";
        return StoreAsync(prefix, photos, cancellationToken);
    }

    public Task<StoredPhotoSet> StoreProofOfWorkPhotosAsync(int projectId, int delayId, int cycleId, DateTimeOffset occurredAt,
        IReadOnlyList<ReportPhotoUploadRecord> photos, CancellationToken cancellationToken)
    {
        var prefix = $"project-{projectId}/{occurredAt:yyyy-MM-dd}/report-{delayId}/proof-of-work/cycle-{cycleId}";
        return StoreAsync(prefix, photos, cancellationToken);
    }

    public Task RemoveAsync(StoredPhotoSet photos)
    {
        return DeleteAsync(photos.StorageKeys);
    }

    public string GetViewUrl(string storedUrl)
    {
        if (IsInContainer(storedUrl, out var uri))
        {
            var blobName = Uri.UnescapeDataString(uri.AbsolutePath[(Container.Uri.AbsolutePath.Length + 1)..]);
            return Container.GetBlobClient(blobName).GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).AbsoluteUri;
        }

        return storedUrl;
    }

    #region Private Methods

    private async Task<StoredPhotoSet> StoreAsync(string prefix, IReadOnlyList<ReportPhotoUploadRecord> photos,
        CancellationToken cancellationToken)
    {
        if (photos.Count == 0)
        {
            return StoredPhotoSet.Empty;
        }

        var attempted = new List<string>();
        try
        {
            // Containers remain private. Read URLs are signed when the API returns photos.
            await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            var stored = new List<ReportPhotoRecord>();
            foreach (var photo in photos)
            {
                stored.Add(await StorePhotoAsync(photo, prefix, attempted, cancellationToken));
            }

            return new StoredPhotoSet(stored, attempted);
        }
        catch
        {
            await DeleteAsync(attempted);
            throw;
        }
    }

    private async Task<ReportPhotoRecord> StorePhotoAsync(ReportPhotoUploadRecord photo, string prefix, List<string> attempted,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var originalName = $"{prefix}/{id}/original";
        var thumbnailName = $"{prefix}/{id}/thumbnail.jpg";
        // Include partially successful uploads in failure cleanup.
        attempted.Add(originalName);
        attempted.Add(thumbnailName);
        await using var original = photo.OpenOriginal();
        var originalUrl = await UploadAsync(originalName, original, photo.ContentType, cancellationToken);
        await using var thumbnail = photo.OpenThumbnail();
        var thumbnailUrl = await UploadAsync(thumbnailName, thumbnail, "image/jpeg", cancellationToken);
        return new ReportPhotoRecord(originalUrl, thumbnailUrl, photo.FileName, photo.Latitude, photo.Longitude);
    }

    private async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var blob = Container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, cancellationToken);
        return blob.Uri.AbsoluteUri;
    }

    private async Task DeleteAsync(IEnumerable<string> blobNames)
    {
        foreach (var name in blobNames)
        {
            try { await Container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: CancellationToken.None); }
            catch (Exception error) { logger.LogWarning(error, "Could not clean up failed delay upload {BlobName}", name); }
        }
    }

    // The path is checked before the container, so existing external fixture images do not require a storage connection.
    private bool IsInContainer(string storedUrl, [NotNullWhen(true)] out Uri? uri)
    {
        return Uri.TryCreate(storedUrl, UriKind.Absolute, out uri)
            && uri.AbsolutePath.Contains($"/{options.Value.ContainerName}/", StringComparison.Ordinal)
            && Container.Uri.IsBaseOf(uri);
    }

    #endregion
}
