using Accessors;
using Accessors.Delays;
using Accessors.Speech;
using Common.Errors;
using Common.Results;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
using DataContract.Mapping.DomainToClientMapping;
using Engines.Delays;
using Engines;
using Engines.Authorization;
using Utilities;
using Utilities.OperationContract;

namespace Managers.Delays;

/// <summary>
/// Orchestrates the delay workflow. Each operation loads what the decision needs, lets an Engine
/// decide, and hands the outcome to an Accessor; the rules themselves live in the Engines.
/// Every expected failure is returned in the result. Identifiers arrive already checked by the
/// API's [PositiveId] attributes; the authorization Engine throws when the user may not see the project.
/// </summary>
public sealed class DelayManager(AccessorFactory accessorFactory, UtilityFactory utilityFactory, EngineFactory engineFactory) : IDelayManager
{
    private readonly IAuthorizationEngine _authorizationEngine = engineFactory.CreateEngine<IAuthorizationEngine>();
    private readonly IDelayAccessor _delayAccessor = accessorFactory.CreateAccessor<IDelayAccessor>();
    private readonly IDelayWorkflowAccessor _workflowAccessor = accessorFactory.CreateAccessor<IDelayWorkflowAccessor>();
    private readonly ISpeechAccessor _speechAccessor = accessorFactory.CreateAccessor<ISpeechAccessor>();
    private readonly IAzureBlobStorageUtility _blobStorage = utilityFactory.CreateUtility<IAzureBlobStorageUtility>();

    public async Task<Result<TrackingResponse>> GetProjectTrackingDetailsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);
        var data = await _delayAccessor.GetProjectTrackingDetailsAsync(projectId, cancellationToken);
        return DelayTrackingEngine.BuildTracking(data, access, _blobStorage.GetReadUrl);
    }

    public async Task<Result<TrackingDelayResponse>> GetDelayAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);
        var data = await _delayAccessor.GetDelayTrackingDetailsAsync(projectId, delayId, cancellationToken);
        var delay = DelayTrackingEngine.BuildDelays(data, access, _blobStorage.GetReadUrl).SingleOrDefault();
        if (delay is null)
        {
            return DelayErrors.NotFound(delayId);
        }

        return delay;
    }

    public async Task<Result<DelayReportOptionsResponse>> GetReportOptionsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await GetReporterAccessAsync(userId, projectId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var options = await _delayAccessor.GetReportOptionsAsync(projectId, cancellationToken);
        return options.ToResponse(access.Data);
    }

    public async Task<Result<DelaySpeechResponse>> TranscribeReportAsync(int userId, int projectId, DelaySpeechRequest request, CancellationToken cancellationToken)
    {
        var access = await GetReporterAccessAsync(userId, projectId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var choices = await _delayAccessor.GetReportChoicesAsync(projectId, cancellationToken);
        await using var audio = request.Audio.OpenReadStream();
        var transcript = await _speechAccessor.TranscribeAsync(audio, cancellationToken);

        return DelaySpeechEngine.MapFields(transcript, choices);
    }

    public async Task<Result<DelaySummaryResponse>> RaiseAsync(int userId, int projectId, RaiseDelayRequest request, CancellationToken cancellationToken)
    {
        var access = await GetReporterAccessAsync(userId, projectId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var options = await _delayAccessor.GetReportOptionsAsync(projectId, cancellationToken);
        var write = DelayReportEngine.Raise(userId, projectId, request, options, DateTimeOffset.UtcNow);
        if (write.IsFailure)
        {
            return write.Error;
        }

        var saved = await _delayAccessor.CreateDelayAsync(write.Data, cancellationToken);

        // No external effects or fallible database reads after a successful commit.
        return write.Data.ToSummaryResponse(saved, access.Data, options);
    }

    public async Task<Result<DelayActionResponse>> AcknowledgeAsync(int userId, int projectId, int delayId, AcknowledgeDelayRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.Acknowledge(context.Data, request), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> ReviseCompletionTimeAsync(int userId, int projectId, int delayId, ReviseCompletionTimeRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.ReviseCompletionTime(context.Data, request), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> RequestHandoverCheckAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.RequestHandoverCheck(context.Data), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> VerifyHandoverAsync(int userId, int projectId, int delayId, int impactId, VerifyHandoverRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.VerifyHandover(context.Data, impactId, request), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> ResolveAsync(int userId, int projectId, int delayId, ResolveDelayRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.Resolve(context.Data, request), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> ReopenAsync(int userId, int projectId, int delayId, ReopenDelayRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.Reopen(context.Data, request), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> RevokeAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(DelayWorkflowEngine.Revoke(context.Data), cancellationToken);
    }

    #region Private Methods

    private async Task<Result<ProjectAccessRecord>> GetReporterAccessAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);
        if (DelayReportEngine.CheckReporter(access) is { } refused)
        {
            return refused;
        }

        return access;
    }

    private async Task<Result<DelayWorkflowContext>> LoadWorkflowAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var access = await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);
        var state = await _workflowAccessor.GetWorkflowStateAsync(projectId, delayId, cancellationToken);
        if (state is null)
        {
            return DelayErrors.NotFound(delayId);
        }

        return new DelayWorkflowContext(userId, access, state, DateTimeOffset.UtcNow);
    }

    /// <summary>Applies the Engine's writes, or passes its refusal on without touching the database.</summary>
    private async Task<Result<DelayActionResponse>> ApplyAsync(Result<IReadOnlyList<DelayWorkflowWrite>> planned, CancellationToken cancellationToken)
    {
        if (planned.IsFailure)
        {
            return planned.Error;
        }

        var applied = await _workflowAccessor.ApplyAsync(planned.Data, cancellationToken);
        if (applied.IsFailure)
        {
            return applied.Error;
        }

        return planned.Data[^1].ToResponse(applied.Data);
    }

    #endregion
}
