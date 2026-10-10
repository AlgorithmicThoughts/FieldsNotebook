using System.ComponentModel.DataAnnotations;
using DataContract.Users;
using Managers.Users;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController(IUserManager userManager) : ControllerBase
    {
        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> GetUser([FromRoute, Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
        {
            var user = await userManager.GetUserAsync(id, cancellationToken);
            return user is null ? NotFound() : user;
        }
    }
}
