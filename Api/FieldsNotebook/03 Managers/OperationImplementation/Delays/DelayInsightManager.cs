using Accessors;
using Accessors.Activity;
using Accessors.Delays;
using Accessors.Users;
using Accessors.WorkItems;
using Common.Errors;
using Common.Results;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.WorkItems;
using DataContract.Home;
using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Reports;
using DataContract.Upcoming;
using Engines;
using Engines.Authorization;
using Engines.Crews;
using Engines.Delays;
using Utilities;
using Utilities.OperationContract;

namespace Managers.Delays;

/// <summary>
/// Orchestrates the read views of delays. The authorization Engine throws when the user may not see a project;
/// every other expected failure is returned in the result.
/// </summary>
public sealed class DelayInsightManager(AccessorFactory accessorFactory, EngineFactory engineFactory, UtilityFactory utilityFactory)
    : IDelayInsightManager
{
    private readonly IAuthorizationEngine _authorizationEngine = engineFactory.CreateEngine<IAuthorizationEngine>();
    private readonly IDelayInsightEngine _insightEngine = engineFactory.CreateEngine<IDelayInsightEngine>();
    private readonly ICrewCoverageEngine _crewCoverageEngine = engineFactory.CreateEngine<ICrewCoverageEngine>();
    private readonly IDelayAccessor _delayAccessor = accessorFactory.CreateAccessor<IDelayAccessor>();
    private readonly IActivityAccessor _activityAccessor = accessorFactory.CreateAccessor<IActivityAccessor>();
    private readonly IWorkItemAccessor _workItemAccessor = accessorFactory.CreateAccessor<IWorkItemAccessor>();
    private readonly IDelayPhotoAccessor _photoAccessor = accessorFactory.CreateAccessor<IDelayPhotoAccessor>();
    private readonly IUserAccessor _userAccessor = accessorFactory.CreateAccessor<IUserAccessor>();
    private readonly IClockUtility _clock = utilityFactory.CreateUtility<IClockUtility>();

    public async Task<Result<HomeResponse>> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var ourCrewIds = _crewCoverageEngine.GetCoveredCrewIds(access);
        if (ourCrewIds.Count == 0)
        {
            return _insightEngine.BuildHome(ourCrewIds, ActiveDelaySet.Empty, []).ToResponse(access);
        }

        // Sequential on purpose: both Accessors share this request's DbContext.
        var active = await _delayAccessor.FindActiveDelaysAsync(projectId, ourCrewIds, cancellationToken);
        var activity = await _activityAccessor.GetRecentActivityForCrewsAsync(projectId, ourCrewIds, _insightEngine.RecentActivityLimit,
            cancellationToken);

        return _insightEngine.BuildHome(ourCrewIds, active, activity).ToResponse(access);
    }

    public async Task<Result<UpcomingResponse>> GetUpcomingAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var ourCrewIds = _crewCoverageEngine.GetCoveredCrewIds(access);
        if (ourCrewIds.Count == 0)
        {
            return _insightEngine.BuildUpcoming(ActiveDelaySet.Empty, PlannedWorkSet.Empty).ToResponse();
        }

        // Sequential on purpose: both Accessors share this request's DbContext.
        var active = await _delayAccessor.FindActiveDelaysAsync(projectId, ourCrewIds, cancellationToken);
        if (active.Delays.Count == 0)
        {
            return _insightEngine.BuildUpcoming(active, PlannedWorkSet.Empty).ToResponse();
        }

        var planned = await _workItemAccessor.GetPlannedWorkAsync(projectId, _insightEngine.PlannedWorkCutoff(_clock.UtcNow), cancellationToken);

        return _insightEngine.BuildUpcoming(active, planned).ToResponse();
    }

    public async Task<Result<TrackingResponse>> GetProjectTrackingDetailsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var history = await _delayAccessor.GetDelayHistoryAsync(projectId, null, cancellationToken);
        return _insightEngine.BuildTracking(history).ToTrackingResponse(access, _photoAccessor.GetViewUrl);
    }

    public async Task<Result<TrackingDelayResponse>> GetDelayAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var history = await _delayAccessor.GetDelayHistoryAsync(projectId, delayId, cancellationToken);
        if (_insightEngine.BuildTracking(history).SingleOrDefault() is not { } delay)
        {
            return DelayErrors.NotFound(delayId);
        }

        return delay.ToResponse(access, _photoAccessor.GetViewUrl);
    }

    public async Task<Result<IReadOnlyList<DelayReportRowResponse>>> GetDelayReportAsync(int userId, DelayReportRequest request, CancellationToken cancellationToken)
    {
        var query = request.ToDomain();

        // Sequential on purpose: every call shares this request's DbContext.
        // A report must not widen past the projects the user can open, so one denial fails the whole scope.
        foreach (var projectId in query.ProjectIds)
        {
            await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);
        }

        var report = await _delayAccessor.FindDelaysAsync(query, cancellationToken);
        return Result<IReadOnlyList<DelayReportRowResponse>>.Success(report.ToResponse());
    }

    public async Task<Result<IReadOnlyList<string>>> GetDelayTagNamesAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _userAccessor.GetUserAsync(userId, cancellationToken);
        if (user is not { IsActive: true })
        {
            return AuthorizationErrors.UserNotActive();
        }

        var tags = await _delayAccessor.GetDelayTagsAsync(cancellationToken);
        return Result<IReadOnlyList<string>>.Success(tags.Select(t => t.Name).ToList());
    }
}
