using Common.Results;
using DataContract.Upcoming;

namespace Managers.Upcoming;

public interface IUpcomingManager
{
    /// <summary>Blocked work for the user's crews on the next project day; refused when the user has no active access.</summary>
    Task<Result<UpcomingResponse>> GetUpcomingAsync(int userId, int projectId, CancellationToken cancellationToken);
}
