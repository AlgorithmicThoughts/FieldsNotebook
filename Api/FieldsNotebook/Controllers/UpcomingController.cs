using DataContract.Upcoming;
using Managers.Upcoming;
using Managers;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/upcoming")]
    public class UpcomingController(ManagerFactory managerFactory) : ControllerBase
    {
        [HttpGet(Name = "GetProjectUpcoming")]
        [ProducesResponseType<UpcomingResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<UpcomingResponse> GetUpcoming(int projectId)
        {
            var manager = managerFactory.CreateManager<IUpcomingManager>();
            return manager.GetUpcomingAsync(DemoIdentity.GetUserId(HttpContext), projectId, HttpContext.RequestAborted);
        }
    }
}
