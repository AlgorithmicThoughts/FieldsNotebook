using DataContract.Home;
using Managers.Home;
using Managers;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/home")]
    public class HomeController(ManagerFactory managerFactory) : ControllerBase
    {
        [HttpGet(Name = "GetProjectHome")]
        [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<HomeResponse> GetHome(int projectId)
        {
            var manager = managerFactory.CreateManager<IHomeManager>();
            return manager.GetHomeAsync(DemoIdentity.GetUserId(HttpContext), projectId, HttpContext.RequestAborted);
        }
    }
}
