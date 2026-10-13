namespace DataContract.Domain.Delays;

/// <summary>Photos the photo Accessor stored, and the keys it needs to remove them again if the write that records them fails.</summary>
/// <param name="StorageKeys">Opaque to every caller but the photo Accessor.</param>
public sealed record StoredPhotoSet(
    IReadOnlyList<ReportPhotoRecord> Photos,
    IReadOnlyList<string> StorageKeys)
{
    public static StoredPhotoSet Empty { get; } = new([], []);
}
