using System.ComponentModel.DataAnnotations;
using DataContract.Home;
using Managers.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/home")]
    public class HomeController(IHomeManager homeManager) : ControllerBase
    {
        [HttpGet(Name = "GetProjectHome")]
        [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<HomeResponse> GetHome(
            [FromRoute, Range(1, int.MaxValue)] int projectId,
            [FromHeader(Name = DemoIdentity.HeaderName), BindRequired, Range(1, int.MaxValue)] int demoUserId,
            CancellationToken cancellationToken) =>
            homeManager.GetHomeAsync(demoUserId, projectId, cancellationToken);
    }
}
