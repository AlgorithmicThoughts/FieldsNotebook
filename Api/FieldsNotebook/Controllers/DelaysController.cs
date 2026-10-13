using DataContract.Delays;
using DataContract.Home;
using DataContract.Validation;
using FieldsNotebook.Configuration;
using Managers;
using Managers.Delays;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

/// <summary>
/// Delays are a collection under their project. Each workflow action creates a sub-resource of the
/// delay, such as an acknowledgment, a handover request or a verification, rather than calling a verb.
/// Identifiers below one fail [PositiveId] with a 400 before the Manager runs; every other expected
/// failure comes back in the Manager's result.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:int}/delays")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class DelaysController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IDelayManager _delayManager = managerFactory.CreateManager<IDelayManager>();
    private readonly IDelayInsightManager _insightManager = managerFactory.CreateManager<IDelayInsightManager>();

    [HttpGet(Name = "GetDelays")]
    [ProducesResponseType<TrackingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackingResponse>> GetDelays([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _insightManager.GetProjectTrackingDetailsAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{delayId:int}", Name = "GetDelay")]
    [ProducesResponseType<TrackingDelayResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackingDelayResponse>> GetDelay([PositiveId] int projectId, [PositiveId] int delayId, CancellationToken cancellationToken)
    {
        var result = await _insightManager.GetDelayAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("report-options", Name = "GetDelayReportOptions")]
    [ProducesResponseType<DelayReportOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DelayReportOptionsResponse>> GetReportOptions([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _delayManager.GetReportOptionsAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("transcriptions", Name = "TranscribeDelay")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DelaySpeechResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DelaySpeechResponse>> Transcribe([PositiveId] int projectId, [FromForm] DelaySpeechRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.TranscribeReportAsync(DemoIdentity.GetUserId(HttpContext), projectId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost(Name = "RaiseDelay")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestLimits.DelayPhotoUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimits.DelayPhotoUploadBytes)]
    [ProducesResponseType<DelaySummaryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DelaySummaryResponse>> Raise([PositiveId] int projectId, [FromForm] RaiseDelayRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.RaiseAsync(DemoIdentity.GetUserId(HttpContext), projectId, request, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtRoute("GetDelay", new { projectId, delayId = result.Data.Id }, result.Data);
    }

    [HttpPost("{delayId:int}/acknowledgments", Name = "AcknowledgeDelay")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> Acknowledge([PositiveId] int projectId, [PositiveId] int delayId, AcknowledgeDelayRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.AcknowledgeAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    // A collection, not PUT on the time: every revision appends its own event, so repeating one is not idempotent.
    [HttpPost("{delayId:int}/completion-time-revisions", Name = "ReviseDelayCompletionTime")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> ReviseCompletionTime([PositiveId] int projectId, [PositiveId] int delayId, ReviseCompletionTimeRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.ReviseCompletionTimeAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("{delayId:int}/handover-requests", Name = "RequestDelayHandoverCheck")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> RequestHandoverCheck([PositiveId] int projectId, [PositiveId] int delayId, CancellationToken cancellationToken)
    {
        var result = await _delayManager.RequestHandoverCheckAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("{delayId:int}/impacts/{impactId:int}/verifications", Name = "VerifyDelayHandover")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> VerifyHandover([PositiveId] int projectId, [PositiveId] int delayId, [PositiveId] int impactId, VerifyHandoverRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.VerifyHandoverAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, impactId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("{delayId:int}/resolutions", Name = "ResolveDelay")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestLimits.DelayPhotoUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimits.DelayPhotoUploadBytes)]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> Resolve([PositiveId] int projectId, [PositiveId] int delayId, [FromForm] ResolveDelayRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.ResolveAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    // A reopen is the creation of the delay's next cycle.
    [HttpPost("{delayId:int}/cycles", Name = "ReopenDelay")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> Reopen([PositiveId] int projectId, [PositiveId] int delayId, ReopenDelayRequest request, CancellationToken cancellationToken)
    {
        var result = await _delayManager.ReopenAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    // Not DELETE: a revoked delay still exists and still reads back, with status Revoked.
    [HttpPost("{delayId:int}/revocations", Name = "RevokeDelay")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelayActionResponse>> Revoke([PositiveId] int projectId, [PositiveId] int delayId, CancellationToken cancellationToken)
    {
        var result = await _delayManager.RevokeAsync(DemoIdentity.GetUserId(HttpContext), projectId, delayId, cancellationToken);

        return this.ToActionResult(result);
    }
}
