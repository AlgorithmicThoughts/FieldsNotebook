using Accessors;
using Accessors.Activity;
using Accessors.Delays;
using Common.Results;
using DataContract.Domain.Delays;
using DataContract.Home;
using Engines.Crews;
using Engines.Home;
using Engines;
using Engines.Authorization;

namespace Managers.Home;

public sealed class HomeManager(AccessorFactory accessorFactory, EngineFactory engineFactory) : IHomeManager
{
    private readonly IAuthorizationEngine _authorizationEngine = engineFactory.CreateEngine<IAuthorizationEngine>();
    private readonly IDelayAccessor _delayAccessor = accessorFactory.CreateAccessor<IDelayAccessor>();
    private readonly IActivityAccessor _activityAccessor = accessorFactory.CreateAccessor<IActivityAccessor>();

    public async Task<Result<HomeResponse>> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var ourCrewIds = CrewCoverageEngine.GetCoveredCrewIds(access);
        if (ourCrewIds.Count == 0)
        {
            return HomeEngine.BuildHome(access, ourCrewIds, ActiveDelaySet.Empty, []);
        }

        // Sequential on purpose: both Accessors share this request's DbContext.
        var active = await _delayAccessor.GetActiveDelaysForCrewsAsync(projectId, ourCrewIds, cancellationToken);
        var activity = await _activityAccessor.GetRecentActivityForCrewsAsync(projectId, ourCrewIds, HomeEngine.RecentActivityLimit, cancellationToken);

        return HomeEngine.BuildHome(access, ourCrewIds, active, activity);
    }
}
