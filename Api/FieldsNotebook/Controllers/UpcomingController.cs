using DataContract.Upcoming;
using DataContract.Validation;
using Managers;
using Managers.Upcoming;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/upcoming")]
public class UpcomingController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IUpcomingManager _upcomingManager = managerFactory.CreateManager<IUpcomingManager>();

    [HttpGet(Name = "GetProjectUpcoming")]
    [ProducesResponseType<UpcomingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UpcomingResponse>> GetUpcoming([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _upcomingManager.GetUpcomingAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }
}
