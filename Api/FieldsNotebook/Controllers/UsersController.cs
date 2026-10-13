using DataContract.Users;
using DataContract.Validation;
using Managers;
using Managers.Users;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IUserManager _userManager = managerFactory.CreateManager<IUserManager>();

    [HttpGet("{userId:int}", Name = "GetUser")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser([PositiveId] int userId, CancellationToken cancellationToken)
    {
        var result = await _userManager.GetUserAsync(DemoIdentity.GetUserId(HttpContext), userId, cancellationToken);

        return this.ToActionResult(result);
    }
}
