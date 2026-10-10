namespace Accessors.Notifications
{
    public interface INotificationAccessor
    {
        /// <summary>Counts the recipient's notices in the project with no ReadAt.</summary>
        Task<int> CountUnreadAsync(int userId, int projectId, CancellationToken cancellationToken);
    }
}
