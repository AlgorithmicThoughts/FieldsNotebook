using DataContract.Me;
using Managers.Users;
using Managers;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/me")]
    public class MeController(ManagerFactory managerFactory) : ControllerBase
    {
        [HttpGet(Name = "GetCurrentUser")]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
        {
            var manager = managerFactory.CreateManager<IUserManager>();
            var user = await manager.GetCurrentUserAsync(DemoIdentity.GetUserId(HttpContext), HttpContext.RequestAborted);
            return user is null ? NotFound() : user;
        }
    }
}
