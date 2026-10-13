using DataContract.Notifications;
using DataContract.Validation;
using Managers;
using Managers.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/notifications")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class NotificationsController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly INotificationManager _notificationManager = managerFactory.CreateManager<INotificationManager>();

    [HttpGet(Name = "GetNotifications")]
    [ProducesResponseType<NotificationListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListResponse>> GetNotifications([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _notificationManager.GetNotificationsAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("unread-count", Name = "GetUnreadNotificationCount")]
    [ProducesResponseType<UnreadNotificationCountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount([PositiveId] int projectId, CancellationToken cancellationToken)
    {
        var result = await _notificationManager.GetUnreadCountAsync(DemoIdentity.GetUserId(HttpContext), projectId, cancellationToken);

        return this.ToActionResult(result);
    }

    // PUT, not POST: a repeated read keeps the first ReadAt, so the request is idempotent.
    [HttpPut("{notificationId:int}/read", Name = "MarkNotificationRead")]
    [ProducesResponseType<NotificationReadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationReadResponse>> MarkRead([PositiveId] int projectId, [PositiveId] int notificationId,
        CancellationToken cancellationToken)
    {
        var result = await _notificationManager.MarkReadAsync(DemoIdentity.GetUserId(HttpContext), projectId, notificationId, cancellationToken);

        return this.ToActionResult(result);
    }
}
