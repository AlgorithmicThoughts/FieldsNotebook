using DataContract.Notifications;
using Managers.Notifications;
using Managers;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId}/notifications")]
    public class NotificationsController(ManagerFactory managerFactory) : ControllerBase
    {
        [HttpGet(Name = "GetNotifications")]
        [ProducesResponseType<NotificationListResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<NotificationListResponse> GetNotifications(int projectId)
        {
            var manager = managerFactory.CreateManager<INotificationManager>();
            return manager.GetNotificationsAsync(DemoIdentity.GetUserId(HttpContext), projectId, HttpContext.RequestAborted);
        }

        [HttpGet("unread-count", Name = "GetUnreadNotificationCount")]
        [ProducesResponseType<UnreadNotificationCountResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<UnreadNotificationCountResponse> GetUnreadCount(int projectId)
        {
            var manager = managerFactory.CreateManager<INotificationManager>();
            return manager.GetUnreadCountAsync(DemoIdentity.GetUserId(HttpContext), projectId, HttpContext.RequestAborted);
        }

        [HttpPost("{notificationId}/read", Name = "MarkNotificationRead")]
        [ProducesResponseType<NotificationReadResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<NotificationReadResponse> MarkRead(
            int projectId,
            int notificationId)
        {
            var manager = managerFactory.CreateManager<INotificationManager>();
            return manager.MarkReadAsync(DemoIdentity.GetUserId(HttpContext), projectId, notificationId, HttpContext.RequestAborted);
        }
    }
}
