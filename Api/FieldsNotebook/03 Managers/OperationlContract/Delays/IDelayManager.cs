using DataContract.Delays;

namespace Managers.Delays;

public interface IDelayManager
{
    Task<TrackingResponse> GetProjectTrackingDetailsAsync(int userId, int projectId, CancellationToken cancellationToken);
}