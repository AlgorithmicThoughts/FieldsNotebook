using Accessors;
using Accessors.Notifications;
using Common.Errors;
using Common.Results;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Notifications;
using Engines;
using Engines.Authorization;
using Utilities;
using Utilities.OperationContract;

namespace Managers.Notifications;

public sealed class NotificationManager(AccessorFactory accessorFactory, EngineFactory engineFactory, UtilityFactory utilityFactory)
    : INotificationManager
{
    private readonly IAuthorizationEngine _authorizationEngine = engineFactory.CreateEngine<IAuthorizationEngine>();
    private readonly INotificationAccessor _notificationAccessor = accessorFactory.CreateAccessor<INotificationAccessor>();
    private readonly IClockUtility _clock = utilityFactory.CreateUtility<IClockUtility>();

    public async Task<Result<UnreadNotificationCountResponse>> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var unreadCount = await _notificationAccessor.CountUnreadAsync(userId, projectId, cancellationToken);
        return NotificationDomainToClientMapping.ToUnreadCountResponse(unreadCount);
    }

    public async Task<Result<NotificationListResponse>> GetNotificationsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

        var notices = await _notificationAccessor.GetNotificationsAsync(userId, projectId, cancellationToken);
        return notices.ToListResponse();
    }

    public async Task<Result<NotificationReadResponse>> MarkReadAsync(int userId, int projectId, int notificationId, CancellationToken cancellationToken)
    {
        await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);

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
