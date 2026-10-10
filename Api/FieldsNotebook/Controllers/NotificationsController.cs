using System.ComponentModel.DataAnnotations;
using DataContract.Notifications;
using Managers.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/notifications")]
    public class NotificationsController(INotificationManager notificationManager) : ControllerBase
    {
        [HttpGet("unread-count", Name = "GetUnreadNotificationCount")]
        [ProducesResponseType<UnreadNotificationCountResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<UnreadNotificationCountResponse> GetUnreadCount(
            [FromRoute, Range(1, int.MaxValue)] int projectId,
            [FromHeader(Name = DemoIdentity.HeaderName), BindRequired, Range(1, int.MaxValue)] int demoUserId,
            CancellationToken cancellationToken) =>
            notificationManager.GetUnreadCountAsync(demoUserId, projectId, cancellationToken);
    }
}
