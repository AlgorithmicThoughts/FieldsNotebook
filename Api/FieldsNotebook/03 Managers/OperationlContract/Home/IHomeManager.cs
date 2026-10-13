using Common.Results;
using DataContract.Home;

namespace Managers.Home;

public interface IHomeManager
{
    /// <summary>The user's home screen for the project; refused when the user has no active access there.</summary>
    Task<Result<HomeResponse>> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken);
}
