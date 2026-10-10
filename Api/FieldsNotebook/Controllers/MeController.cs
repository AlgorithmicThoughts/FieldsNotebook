using System.ComponentModel.DataAnnotations;
using DataContract.Me;
using Managers.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/me")]
    public class MeController(IUserManager userManager) : ControllerBase
    {
        [HttpGet(Name = "GetCurrentUser")]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(
            [FromHeader(Name = DemoIdentity.HeaderName), BindRequired, Range(1, int.MaxValue)] int demoUserId,
            CancellationToken cancellationToken)
        {
            var user = await userManager.GetCurrentUserAsync(demoUserId, cancellationToken);
            return user is null ? NotFound() : user;
        }
    }
}
