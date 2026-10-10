using Common.Delays;
using Common.Reports;

namespace Accessors.Delays
{
    public interface IDelayAccessor
    {
        Task<DelayReportOptionsRecord> GetReportOptionsAsync(int projectId, CancellationToken cancellationToken);
        Task<ReportSavedRecord> CreateReportAsync(Common.Delays.DelayReportRecord report, CancellationToken cancellationToken);
        Task<TrackingSet> GetProjectTrackingDetailsAsync(int projectId, CancellationToken cancellationToken);
        /// <summary>
        /// Active delays in the project where one of the crews is responsible or affected, newest first,
        /// with every impact of those delays.
        /// </summary>
        Task<ActiveDelaySet> GetActiveDelaysForCrewsAsync(int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken);

        /// <summary>
        /// Every delay in the projects created at or after <paramref name="createdFrom"/> and before
        /// <paramref name="createdBefore"/>, newest first; a null bound leaves that end open.
        /// </summary>
        Task<DelayReportSet> GetDelayReportAsync(
            IReadOnlyCollection<int> projectIds, DateTimeOffset? createdFrom, DateTimeOffset? createdBefore, CancellationToken cancellationToken);

        /// <summary>Active DelayTags names in DelayTags order.</summary>
        Task<IReadOnlyList<string>> GetActiveDelayTagNamesAsync(CancellationToken cancellationToken);
    }
}
