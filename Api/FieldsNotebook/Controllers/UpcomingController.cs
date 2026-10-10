using System.ComponentModel.DataAnnotations;
using DataContract.Upcoming;
using Managers.Upcoming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/upcoming")]
    public class UpcomingController(IUpcomingManager upcomingManager) : ControllerBase
    {
        [HttpGet(Name = "GetProjectUpcoming")]
        [ProducesResponseType<UpcomingResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<UpcomingResponse> GetUpcoming(
            [FromRoute, Range(1, int.MaxValue)] int projectId,
            [FromHeader(Name = DemoIdentity.HeaderName), BindRequired, Range(1, int.MaxValue)] int demoUserId,
            CancellationToken cancellationToken) =>
            upcomingManager.GetUpcomingAsync(demoUserId, projectId, cancellationToken);
    }
}
