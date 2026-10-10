using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Utilities.OperationContract;

namespace Utilities.OperationImplementation;

public sealed class AzureBlobStorageUtility(IOptions<AzureBlobStorageOptions> options) : IAzureBlobStorageUtility
{
    private BlobContainerClient? _container;

    private BlobContainerClient Container => _container ??= new BlobServiceClient(options.Value.ConnectionString)
        .GetBlobContainerClient(options.Value.ContainerName);

    public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        // Containers remain private. Read URLs are signed when the API returns photos.
        await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        var blob = Container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, cancellationToken);
        return blob.Uri.AbsoluteUri;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken) =>
        await Container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public string GetReadUrl(string storedUrl)
    {
        if (!Uri.TryCreate(storedUrl, UriKind.Absolute, out var uri)) return storedUrl;
        // Existing external fixture images do not require a storage connection.
        if (!uri.AbsolutePath.Contains($"/{options.Value.ContainerName}/", StringComparison.Ordinal)) return storedUrl;
        if (!Container.Uri.IsBaseOf(uri)) return storedUrl;
        var blobName = Uri.UnescapeDataString(uri.AbsolutePath[(Container.Uri.AbsolutePath.Length + 1)..]);
        return Container.GetBlobClient(blobName).GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).AbsoluteUri;
    }
}
