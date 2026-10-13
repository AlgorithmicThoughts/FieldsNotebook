using DataContract.Domain.Delays;
using DataContract.Domain.Reports;

namespace Accessors.Delays;

/// <summary>Delays as stored: raising one, finding them, and reading their history.</summary>
public interface IDelayAccessor : IAccessor
{
    /// <summary>
    /// Writes the delay, its first cycle, impacts, photos, report event and notices in one transaction.
    /// The photos are already stored; this records them.
    /// </summary>
    Task<ReportSavedRecord> RaiseDelayAsync(RaiseDelayWrite report, IReadOnlyList<ReportPhotoRecord> photos,
        CancellationToken cancellationToken);

    /// <summary>The active delay reasons in DelayTags order.</summary>
    Task<IReadOnlyList<ReportTagRecord>> GetDelayTagsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Active delays in the project where one of the crews is responsible or affected, newest first,
    /// with every impact of those delays.
    /// </summary>
    Task<ActiveDelaySet> FindActiveDelaysAsync(int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken);

    /// <summary>
    /// The full history of every delay in the project, or of one delay when <paramref name="delayId"/> is set;
    /// an empty set when that delay is not in the project.
    /// </summary>
    Task<TrackingSet> GetDelayHistoryAsync(int projectId, int? delayId, CancellationToken cancellationToken);

    /// <summary>Every delay in the query's projects and creation window, newest first.</summary>
    Task<DelayReportSet> FindDelaysAsync(DelayReportQueryRecord query, CancellationToken cancellationToken);
}
