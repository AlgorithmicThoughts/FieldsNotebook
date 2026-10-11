using DataContract.Delays;
using Managers;
using Managers.Delays;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId}/delays")]
public class DelayController(ManagerFactory managerFactory) : ControllerBase
{
    [HttpPost("transcribe")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DelaySpeechResponse>(StatusCodes.Status200OK)]
    public Task<DelaySpeechResponse> TranscribeAsync(int projectId, [FromForm] DelaySpeechRequest request) =>
        managerFactory.CreateManager<IDelaySpeechManager>().TranscribeAsync(
            DemoIdentity.GetUserId(HttpContext), projectId, request, HttpContext.RequestAborted);

    [HttpGet("report-options")]
    public Task<DelayReportOptionsResponse> GetReportOptionsAsync(int projectId) =>
        managerFactory.CreateManager<IDelayReportManager>().GetReportOptionsAsync(
            DemoIdentity.GetUserId(HttpContext), projectId, HttpContext.RequestAborted);

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(140 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 140 * 1024 * 1024)]
    public Task<DataContract.Home.DelaySummaryResponse> CreateReportAsync(int projectId, [FromForm] DelayReportRequest request) =>
        managerFactory.CreateManager<IDelayReportManager>().CreateReportAsync(
            DemoIdentity.GetUserId(HttpContext), projectId, request, HttpContext.RequestAborted);

    [HttpGet(Name = "GetProjectTrackingDetailsAsync")]
    [ProducesResponseType<TrackingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<TrackingResponse> GetProjectTrackingDetailsAsync(int projectId)
    {
        var manager = managerFactory.CreateManager<IDelayManager>();
        return manager.GetProjectTrackingDetailsAsync(
            DemoIdentity.GetUserId(HttpContext), projectId, HttpContext.RequestAborted);
    }
}
