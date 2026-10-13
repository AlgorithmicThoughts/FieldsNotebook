namespace Accessors.Delays;

/// <summary>Where delay photos are stored. Supply the connection string through user-secrets or an environment variable.</summary>
public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "report-delays";
}
