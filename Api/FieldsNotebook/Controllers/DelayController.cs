using DataContract.Delays;
using Managers;
using Managers.Delays;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId}/delays")]
public class DelayController(ManagerFactory managerFactory) : ControllerBase
{
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