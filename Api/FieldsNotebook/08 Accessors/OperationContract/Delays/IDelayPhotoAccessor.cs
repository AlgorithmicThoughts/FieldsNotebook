using DataContract.Domain.Delays;

namespace Accessors.Delays;

/// <summary>
/// Where delay photos are kept. Callers speak of reports and proof of work; the storage service behind
/// it is this Accessor's concern alone.
/// </summary>
public interface IDelayPhotoAccessor : IAccessor
{
    /// <summary>Stores a new report's photos and thumbnails. A failed upload removes what it already stored, then throws.</summary>
    Task<StoredPhotoSet> StoreReportPhotosAsync(int projectId, int? blockingWorkItemId, DateTimeOffset reportedAt,
        IReadOnlyList<ReportPhotoUploadRecord> photos, CancellationToken cancellationToken);

    /// <summary>Stores the photos that prove a cycle's work. A failed upload removes what it already stored, then throws.</summary>
    Task<StoredPhotoSet> StoreProofOfWorkPhotosAsync(int projectId, int delayId, int cycleId, DateTimeOffset occurredAt,
        IReadOnlyList<ReportPhotoUploadRecord> photos, CancellationToken cancellationToken);

    /// <summary>Removes photos whose write failed. Best effort: a photo that cannot be removed is logged, never thrown.</summary>
    Task RemoveAsync(StoredPhotoSet photos);

    /// <summary>A short-lived URL the client can open for a stored photo; any other URL comes back unchanged.</summary>
    string GetViewUrl(string storedUrl);
}
