using Common.Results;
using DataContract.Delays;
using DataContract.Home;
using DataContract.Reports;
using DataContract.Upcoming;

namespace Managers.Delays;

/// <summary>
/// Every read view of the delays a user may see: home, upcoming blocked work, tracking history and reports.
/// Each is refused when the user has no active access to the projects it covers.
/// </summary>
public interface IDelayInsightManager : IManager
{
    /// <summary>The user's home screen for the project.</summary>
    Task<Result<HomeResponse>> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>Blocked work for the user's crews on the next project day.</summary>
    Task<Result<UpcomingResponse>> GetUpcomingAsync(int userId, int projectId, CancellationToken cancellationToken);

    Task<Result<TrackingResponse>> GetProjectTrackingDetailsAsync(int userId, int projectId, CancellationToken cancellationToken);

    Task<Result<TrackingDelayResponse>> GetDelayAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken);

    /// <summary>Every delay in the requested scope, newest first. The user must have access to each project.</summary>
    Task<Result<IReadOnlyList<DelayReportRowResponse>>> GetDelayReportAsync(int userId, DelayReportRequest request, CancellationToken cancellationToken);

    /// <summary>The approved reason tag names, so a breakdown can show a reason nobody raised as zero. Any active user may read them.</summary>
    Task<Result<IReadOnlyList<string>>> GetDelayTagNamesAsync(int userId, CancellationToken cancellationToken);
}
