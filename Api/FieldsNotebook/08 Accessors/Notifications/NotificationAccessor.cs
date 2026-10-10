using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Notifications
{
    public class NotificationAccessor(FieldNotebookContext context) : INotificationAccessor
    {
        public Task<int> CountUnreadAsync(int userId, int projectId, CancellationToken cancellationToken) =>
            context.Notifications
                .CountAsync(n => n.UserId == userId && n.ProjectId == projectId && n.ReadAt == null, cancellationToken);
    }
}
