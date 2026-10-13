using Accessors;
using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using Common.Results;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Notifications;
using Microsoft.Extensions.Logging;
using Utilities;
using Utilities.OperationContract;

namespace Managers.Notifications;

public sealed class NotificationManager(AccessorFactory accessorFactory, UtilityFactory utilityFactory, ILogger<NotificationManager> logger)
    : INotificationManager
{
    private readonly IProjectAccessor _projectAccessor = accessorFactory.CreateAccessor<IProjectAccessor>();
    private readonly INotificationAccessor _notificationAccessor = accessorFactory.CreateAccessor<INotificationAccessor>();
    private readonly IClockUtility _clock = utilityFactory.CreateUtility<IClockUtility>();

    public async Task<Result<UnreadNotificationCountResponse>> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _projectAccessor.AuthorizeProjectAsync(userId, projectId, logger, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var unreadCount = await _notificationAccessor.CountUnreadAsync(userId, projectId, cancellationToken);
        return NotificationDomainToClientMapping.ToUnreadCountResponse(unreadCount);
    }

    public async Task<Result<NotificationListResponse>> GetNotificationsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _projectAccessor.AuthorizeProjectAsync(userId, projectId, logger, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var notices = await _notificationAccessor.GetNotificationsAsync(userId, projectId, cancellationToken);
        return notices.ToListResponse();
    }

    public async Task<Result<NotificationReadResponse>> MarkReadAsync(int userId, int projectId, int notificationId, CancellationToken cancellationToken)
    {
        var access = await _projectAccessor.AuthorizeProjectAsync(userId, projectId, logger, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        // Sequential on purpose: every call shares this request's DbContext.
        // An already read notice changes nothing and keeps its first ReadAt.
        await _notificationAccessor.MarkReadAsync(userId, projectId, notificationId, _clock.UtcNow, cancellationToken);
        var notice = await _notificationAccessor.GetNotificationAsync(userId, projectId, notificationId, cancellationToken);
        if (notice is null)
        {
            return NotificationErrors.NotFound(notificationId);
        }

        var unreadCount = await _notificationAccessor.CountUnreadAsync(userId, projectId, cancellationToken);
        return notice.ToReadResponse(unreadCount);
    }
}
