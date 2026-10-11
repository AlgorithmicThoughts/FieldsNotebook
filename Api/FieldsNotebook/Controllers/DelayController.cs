using DataContract.Delays;
using Managers;
using Managers.Delays;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId}/delays")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class DelayController(ManagerFactory managerFactory) : ControllerBase
{
    [HttpGet(Name = "GetProjectTrackingDetailsAsync")]
    [ProducesResponseType<TrackingResponse>(StatusCodes.Status200OK)]
    public Task<TrackingResponse> GetProjectTrackingDetailsAsync(int projectId) =>
        Manager.GetProjectTrackingDetailsAsync(UserId, projectId, Aborted);

    [HttpPost("{delayId}/acknowledgment", Name = "AcknowledgeDelayAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> AcknowledgeAsync(int projectId, int delayId, AcknowledgeDelayRequest request) =>
        Manager.AcknowledgeAsync(UserId, projectId, delayId, request, Aborted);

    [HttpPost("{delayId}/completion-time", Name = "ReviseDelayCompletionTimeAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> ReviseCompletionTimeAsync(int projectId, int delayId, ReviseCompletionTimeRequest request) =>
        Manager.ReviseCompletionTimeAsync(UserId, projectId, delayId, request, Aborted);

    [HttpPost("{delayId}/handover-check", Name = "RequestDelayHandoverCheckAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> RequestHandoverCheckAsync(int projectId, int delayId) =>
        Manager.RequestHandoverCheckAsync(UserId, projectId, delayId, Aborted);

    [HttpPost("{delayId}/verifications", Name = "VerifyDelayHandoverAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> VerifyHandoverAsync(int projectId, int delayId, VerifyHandoverRequest request) =>
        Manager.VerifyHandoverAsync(UserId, projectId, delayId, request, Aborted);

    [HttpPost("{delayId}/resolution", Name = "ResolveDelayAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> ResolveAsync(int projectId, int delayId) =>
        Manager.ResolveAsync(UserId, projectId, delayId, Aborted);

    [HttpPost("{delayId}/reopen", Name = "ReopenDelayAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> ReopenAsync(int projectId, int delayId, ReopenDelayRequest request) =>
        Manager.ReopenAsync(UserId, projectId, delayId, request, Aborted);

    [HttpPost("{delayId}/revocation", Name = "RevokeDelayAsync")]
    [ProducesResponseType<DelayActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DelayActionResponse> RevokeAsync(int projectId, int delayId) =>
        Manager.RevokeAsync(UserId, projectId, delayId, Aborted);

    private IDelayManager Manager => managerFactory.CreateManager<IDelayManager>();

    private int UserId => DemoIdentity.GetUserId(HttpContext);

    private CancellationToken Aborted => HttpContext.RequestAborted;
}
