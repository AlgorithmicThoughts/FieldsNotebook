using DataContract.Me;
using Managers;
using Managers.Users;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/me")]
public class MeController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IUserManager _userManager = managerFactory.CreateManager<IUserManager>();

    [HttpGet(Name = "GetCurrentUser")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _userManager.GetCurrentUserAsync(DemoIdentity.GetUserId(HttpContext), cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPatch("settings", Name = "UpdateCurrentUserSettings")]
    [ProducesResponseType<UserSettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSettingsResponse>> UpdateSettings(UserSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _userManager.UpdateSettingsAsync(DemoIdentity.GetUserId(HttpContext), request, cancellationToken);

        return this.ToActionResult(result);
    }
}
