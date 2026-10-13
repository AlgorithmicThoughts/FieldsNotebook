using DataContract.Domain.Delays;
using DataContract.Domain.Reports;

namespace Accessors.Delays
{
    public interface IDelayAccessor
    {
        /// <summary>Everything the report form offers: crews, unfinished work items, segments, reasons, the map and people.</summary>
        Task<DelayReportOptionsRecord> GetReportOptionsAsync(int projectId, CancellationToken cancellationToken);

        /// <summary>Only the project's active crews and the active reasons, for matching a spoken description.</summary>
        Task<ReportChoicesRecord> GetReportChoicesAsync(int projectId, CancellationToken cancellationToken);

        Task<ReportSavedRecord> CreateDelayAsync(RaiseDelayWrite report, CancellationToken cancellationToken);

        Task<TrackingSet> GetProjectTrackingDetailsAsync(int projectId, CancellationToken cancellationToken);

        /// <summary>The tracking details of one delay; an empty set when it is not in the project.</summary>
        Task<TrackingSet> GetDelayTrackingDetailsAsync(int projectId, int delayId, CancellationToken cancellationToken);

        /// <summary>
        /// Active delays in the project where one of the crews is responsible or affected, newest first,
        /// with every impact of those delays.
        /// </summary>
        Task<ActiveDelaySet> GetActiveDelaysForCrewsAsync(int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken);

        /// <summary>Every delay in the query's projects and creation window, newest first.</summary>
        Task<DelayReportSet> GetDelayReportAsync(DelayReportQueryRecord query, CancellationToken cancellationToken);

        /// <summary>Active DelayTags names in DelayTags order.</summary>
        Task<IReadOnlyList<string>> GetActiveDelayTagNamesAsync(CancellationToken cancellationToken);
    }
}
