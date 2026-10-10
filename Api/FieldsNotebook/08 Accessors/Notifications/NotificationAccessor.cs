using Common.Notifications;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Notifications
{
    public class NotificationAccessor(FieldNotebookContext context) : INotificationAccessor
    {
        public Task<int> CountUnreadAsync(int userId, int projectId, CancellationToken cancellationToken) =>
            context.Notifications
                .CountAsync(n => n.UserId == userId && n.ProjectId == projectId && n.ReadAt == null, cancellationToken);

        // Filter and sort before the constructor projection; EF cannot translate either on a constructed record.
        public async Task<IReadOnlyList<NotificationRecord>> GetNotificationsAsync(
            int userId, int projectId, CancellationToken cancellationToken) =>
            await (
                from n in context.Notifications
                join d in context.Delays on n.DelayId equals d.Id
                join e in context.ActivityEvents on n.ActivityEventId equals e.Id
                where n.UserId == userId && n.ProjectId == projectId
                orderby n.CreatedAt descending, n.Id descending
                select new NotificationRecord(n.Id, n.DelayId, d.Number, d.Title, e.EventType, n.CreatedAt, n.ReadAt))
                .ToListAsync(cancellationToken);

        public Task<NotificationRecord?> GetNotificationAsync(
            int userId, int projectId, int notificationId, CancellationToken cancellationToken) =>
            (from n in context.Notifications
             join d in context.Delays on n.DelayId equals d.Id
             join e in context.ActivityEvents on n.ActivityEventId equals e.Id
             where n.Id == notificationId && n.UserId == userId && n.ProjectId == projectId
             select new NotificationRecord(n.Id, n.DelayId, d.Number, d.Title, e.EventType, n.CreatedAt, n.ReadAt))
                .FirstOrDefaultAsync(cancellationToken);

        // One conditional UPDATE: concurrent reads of the same notice cannot overwrite the first ReadAt.
        public async Task<bool> MarkReadAsync(
            int userId, int projectId, int notificationId, DateTimeOffset readAt, CancellationToken cancellationToken) =>
            await context.Notifications
                .Where(n => n.Id == notificationId && n.UserId == userId && n.ProjectId == projectId && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, readAt), cancellationToken) > 0;
    }
}
