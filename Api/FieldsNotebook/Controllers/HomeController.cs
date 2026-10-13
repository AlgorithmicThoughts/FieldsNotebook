using DataContract.Home;
using DataContract.Validation;
using Managers;
using Managers.Home;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/home")]
public class HomeController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IHomeManager _homeManager = managerFactory.CreateManager<IHomeManager>();

    [HttpGet(Name = "GetProjectHome")]
    [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HomeResponse>> GetHome([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _homeManager.GetHomeAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }
}
