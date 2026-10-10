namespace Utilities.OperationContract;

public sealed class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "report-delays";
}

public interface IAzureBlobStorageUtility
{
    Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
    string GetReadUrl(string storedUrl);
}
