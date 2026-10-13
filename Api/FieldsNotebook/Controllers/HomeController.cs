using DataContract.Home;
using DataContract.Validation;
using Managers;
using Managers.Delays;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/home")]
public class HomeController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IDelayInsightManager _insightManager = managerFactory.CreateManager<IDelayInsightManager>();

    [HttpGet(Name = "GetProjectHome")]
    [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HomeResponse>> GetHome([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _insightManager.GetHomeAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }
}
