using Accessors;
using Accessors.Delays;
using Accessors.WorkItems;
using Common.Results;
using DataContract.Domain.Delays;
using DataContract.Domain.WorkItems;
using DataContract.Upcoming;
using Engines.Crews;
using Engines;
using Engines.Authorization;
using Engines.Projects;
using Engines.Upcoming;

namespace Managers.Upcoming;

public sealed class UpcomingManager(AccessorFactory accessorFactory, EngineFactory engineFactory) : IUpcomingManager
{
    private readonly IAuthorizationEngine _authorizationEngine = engineFactory.CreateEngine<IAuthorizationEngine>();
    private readonly IDelayAccessor _delayAccessor = accessorFactory.CreateAccessor<IDelayAccessor>();
    private readonly IWorkItemAccessor _workItemAccessor = accessorFactory.CreateAccessor<IWorkItemAccessor>();

    public async Task<Result<UpcomingResponse>> GetUpcomingAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var ourCrewIds = CrewCoverageEngine.GetCoveredCrewIds(access);
        if (ourCrewIds.Count == 0)
        {
            return UpcomingEngine.BuildUpcoming(ActiveDelaySet.Empty, PlannedWorkSet.Empty);
        }

        // Sequential on purpose: both Accessors share this request's DbContext.
        var active = await _delayAccessor.GetActiveDelaysForCrewsAsync(projectId, ourCrewIds, cancellationToken);
        if (active.Delays.Count == 0)
        {
            return UpcomingEngine.BuildUpcoming(active, PlannedWorkSet.Empty);
        }

        var plannedBefore = ProjectCalendarEngine.EndOfNextProjectDay(DateTimeOffset.UtcNow);
        var planned = await _workItemAccessor.GetPlannedWorkAsync(projectId, plannedBefore, cancellationToken);

        return UpcomingEngine.BuildUpcoming(active, planned);
    }
}
