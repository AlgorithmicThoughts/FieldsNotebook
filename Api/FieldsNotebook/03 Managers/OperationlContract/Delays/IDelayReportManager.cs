using DataContract.Delays;
using DataContract.Home;

namespace Managers.Delays;

public interface IDelayReportManager
{
    Task<DelayReportOptionsResponse> GetReportOptionsAsync(int userId, int projectId, CancellationToken cancellationToken);
    Task<DelaySummaryResponse> CreateReportAsync(int userId, int projectId, DelayReportRequest request, CancellationToken cancellationToken);
}
