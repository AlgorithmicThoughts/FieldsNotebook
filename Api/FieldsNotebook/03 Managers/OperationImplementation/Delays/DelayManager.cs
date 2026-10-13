using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Accessors.Speech;
using Accessors.WorkItems;
using Common.Errors;
using Common.Results;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Mapping.DomainToClientMapping;
using Engines;
using Engines.Delays;
using Microsoft.Extensions.Logging;
using Utilities;
using Utilities.OperationContract;

namespace Managers.Delays;

/// <summary>
/// Orchestrates the delay workflow. Each operation loads what the decision needs, lets the workflow Engine
/// decide, and hands the outcome to the Accessors; the rules themselves live in the Engine. Photos are stored
/// before the write that records them and removed again if that write does not commit.
/// Every expected failure is returned in the result. Identifiers arrive already checked by the API's [PositiveId] attributes.
/// </summary>
public sealed class DelayManager(AccessorFactory accessorFactory, EngineFactory engineFactory, UtilityFactory utilityFactory,
    ILogger<DelayManager> logger) : IDelayManager
{
    private readonly IDelayWorkflowEngine _workflowEngine = engineFactory.CreateEngine<IDelayWorkflowEngine>();
    private readonly IDelaySpeechEngine _speechEngine = engineFactory.CreateEngine<IDelaySpeechEngine>();
    private readonly IProjectAccessor _projectAccessor = accessorFactory.CreateAccessor<IProjectAccessor>();
    private readonly IWorkItemAccessor _workItemAccessor = accessorFactory.CreateAccessor<IWorkItemAccessor>();
    private readonly IDelayAccessor _delayAccessor = accessorFactory.CreateAccessor<IDelayAccessor>();
    private readonly IDelayWorkflowAccessor _workflowAccessor = accessorFactory.CreateAccessor<IDelayWorkflowAccessor>();
    private readonly IDelayPhotoAccessor _photoAccessor = accessorFactory.CreateAccessor<IDelayPhotoAccessor>();
    private readonly ISpeechAccessor _speechAccessor = accessorFactory.CreateAccessor<ISpeechAccessor>();
    private readonly IClockUtility _clock = utilityFactory.CreateUtility<IClockUtility>();

    public async Task<Result<DelayReportOptionsResponse>> GetReportOptionsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await GetReporterAccessAsync(userId, projectId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var options = await LoadReportOptionsAsync(projectId, cancellationToken);
        return options.ToResponse(access.Data);
    }

    public async Task<Result<DelaySpeechResponse>> TranscribeReportAsync(int userId, int projectId, DelaySpeechRequest request, CancellationToken cancellationToken)
    {
        var access = await GetReporterAccessAsync(userId, projectId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        // Sequential on purpose: every Accessor shares this request's DbContext.
        var crews = await _projectAccessor.GetCrewsAsync(projectId, cancellationToken);
        var tags = await _delayAccessor.GetDelayTagsAsync(cancellationToken);
        await using var audio = request.ToDomain().OpenAudio();
        var transcript = await _speechAccessor.TranscribeAsync(audio, cancellationToken);

        return _speechEngine.SuggestFields(transcript, new ReportChoicesRecord(crews, tags)).ToResponse();
    }

    public async Task<Result<DelaySummaryResponse>> RaiseAsync(int userId, int projectId, RaiseDelayRequest request, CancellationToken cancellationToken)
    {
        var access = await GetReporterAccessAsync(userId, projectId, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var options = await LoadReportOptionsAsync(projectId, cancellationToken);
        var write = _workflowEngine.Raise(userId, projectId, request.ToDomain(), options, _clock.UtcNow);
        if (write.IsFailure)
        {
            return write.Error;
        }

        var report = write.Data;
        var photos = await _photoAccessor.StoreReportPhotosAsync(projectId, report.BlockingWorkItemId, report.CreatedAt, report.Photos,
            cancellationToken);
        ReportSavedRecord saved;
        try
        {
            saved = await _delayAccessor.RaiseDelayAsync(report, photos.Photos, cancellationToken);
        }
        catch
        {
            await _photoAccessor.RemoveAsync(photos);
            throw;
        }

        // No external effects or fallible database reads after a successful commit.
        return report.ToSummaryResponse(saved, access.Data, options);
    }

    public async Task<Result<DelayActionResponse>> AcknowledgeAsync(int userId, int projectId, int delayId, AcknowledgeDelayRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.Acknowledge(context.Data, request.ToDomain(), request.ExpectedReadyAt), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> ReviseCompletionTimeAsync(int userId, int projectId, int delayId, ReviseCompletionTimeRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.ReviseCompletionTime(context.Data, request.ExpectedReadyAt), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> RequestHandoverCheckAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.RequestHandoverCheck(context.Data), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> VerifyHandoverAsync(int userId, int projectId, int delayId, int impactId, VerifyHandoverRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.VerifyHandover(context.Data, request.ToDomain(impactId)), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> ResolveAsync(int userId, int projectId, int delayId, ResolveDelayRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.Resolve(context.Data, request.Photos.ToDomain()), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> ReopenAsync(int userId, int projectId, int delayId, ReopenDelayRequest request, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.Reopen(context.Data, request.Reason), cancellationToken);
    }

    public async Task<Result<DelayActionResponse>> RevokeAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var context = await LoadWorkflowAsync(userId, projectId, delayId, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        return await ApplyAsync(_workflowEngine.Revoke(context.Data), cancellationToken);
    }

    #region Private Methods

    private async Task<Result<ProjectAccessRecord>> GetReporterAccessAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _projectAccessor.AuthorizeProjectAsync(userId, projectId, logger, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        if (_workflowEngine.CheckReporter(access.Data) is { } refused)
        {
            return refused;
        }

        return access;
    }

    /// <summary>Everything the report form offers, read from the Resources that own each part. Sequential: one DbContext.</summary>
    private async Task<DelayReportOptionsRecord> LoadReportOptionsAsync(int projectId, CancellationToken cancellationToken)
    {
        var crews = await _projectAccessor.GetCrewsAsync(projectId, cancellationToken);
        var workItems = await _workItemAccessor.GetOpenWorkItemsAsync(projectId, cancellationToken);
        var segments = await _projectAccessor.GetSegmentsAsync(projectId, cancellationToken);
        var tags = await _delayAccessor.GetDelayTagsAsync(cancellationToken);
        var siteMap = await _projectAccessor.GetActiveSiteMapAsync(projectId, cancellationToken);
        var people = await _projectAccessor.GetMembersAsync(projectId, cancellationToken);

        return new DelayReportOptionsRecord(crews, workItems, segments, tags, siteMap, people);
    }

    private async Task<Result<DelayWorkflowContext>> LoadWorkflowAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken)
    {
        var access = await _projectAccessor.AuthorizeProjectAsync(userId, projectId, logger, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var state = await _workflowAccessor.GetWorkflowStateAsync(projectId, delayId, cancellationToken);
        if (state is null)
        {
            return DelayErrors.NotFound(delayId);
        }

        var contacts = await _projectAccessor.GetCrewContactsAsync(projectId, cancellationToken);
        return new DelayWorkflowContext(userId, access.Data, state, _clock.UtcNow, contacts);
    }

    /// <summary>
    /// Stores any proof-of-work photos, applies the Engine's writes, and removes the photos again when the write
    /// is refused or fails. An Engine refusal passes on without touching a Resource.
    /// </summary>
    private async Task<Result<DelayActionResponse>> ApplyAsync(Result<IReadOnlyList<DelayWorkflowWrite>> planned, CancellationToken cancellationToken)
    {
        if (planned.IsFailure)
        {
            return planned.Error;
        }

        var photos = await StoreProofOfWorkAsync(planned.Data, cancellationToken);
        Result<DelayWorkflowResult> applied;
        try
        {
            applied = await _workflowAccessor.ApplyAsync(planned.Data, photos.Photos, cancellationToken);
        }
        catch
        {
            await _photoAccessor.RemoveAsync(photos);
            throw;
        }

        if (applied.IsFailure)
        {
            await _photoAccessor.RemoveAsync(photos);
            return applied.Error;
        }

        return planned.Data[^1].ToResponse(applied.Data);
    }

    private async Task<StoredPhotoSet> StoreProofOfWorkAsync(IReadOnlyList<DelayWorkflowWrite> writes, CancellationToken cancellationToken)
    {
        // Only a resolution carries proof of work, and it closes the cycle it names.
        if (writes.SingleOrDefault(w => w.ProofOfWorkPhotos.Count > 0) is not { Cycle.CycleId: { } cycleId } step)
        {
            return StoredPhotoSet.Empty;
        }

        return await _photoAccessor.StoreProofOfWorkPhotosAsync(step.ProjectId, step.DelayId, cycleId, step.OccurredAt,
            step.ProofOfWorkPhotos, cancellationToken);
    }

    #endregion
}
