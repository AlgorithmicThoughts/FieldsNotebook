using Common.Delays;

namespace Accessors.Delays
{
    public interface IDelayAccessor
    {
        Task<DelayReportOptionsRecord> GetReportOptionsAsync(int projectId, CancellationToken cancellationToken);
        Task<ReportSavedRecord> CreateReportAsync(DelayReportRecord report, CancellationToken cancellationToken);
        Task<TrackingSet> GetProjectTrackingDetailsAsync(int projectId, CancellationToken cancellationToken);
        /// <summary>
        /// Active delays in the project where one of the crews is responsible or affected, newest first,
        /// with every impact of those delays.
        /// </summary>
        Task<ActiveDelaySet> GetActiveDelaysForCrewsAsync(int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken);
    }
}
