using DataContract.Users;
using Managers.Users;
using Managers;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController(ManagerFactory managerFactory) : ControllerBase
    {
        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> GetUser(int id)
        {
            var manager = managerFactory.CreateManager<IUserManager>();
            var user = await manager.GetUserAsync(id, HttpContext.RequestAborted);
            return user is null ? NotFound() : user;
        }
    }
}
