using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Notifications;
using DataContract.Domain.Projects;
using Managers.Notifications;
using Moq;

namespace Tests.Managers;

public class NotificationManagerTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int OtherProjectId = 2;

    private static readonly DateTimeOffset ReadTime = new(2026, 10, 7, 13, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProjectAccessAccessor> _projectAccess = new();
    private readonly Mock<INotificationAccessor> _notifications = new();

    public NotificationManagerTests()
    {
        _projectAccess
            .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []));
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithAccess_ReturnsRecipientCount()
    {
        // Arrange
        _notifications.Setup(a => a.CountUnreadAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        // Act
        var result = await CreateManager().GetUnreadCountAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Equal(2, ResultAssert.Succeeded(result).UnreadCount);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoProjectAccess_RefusesWithoutCounting()
    {
        // Act
        var act = () => CreateManager().GetUnreadCountAsync(UserId, OtherProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _notifications.Verify(a => a.CountUnreadAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithAccess_ReturnsNoticesAndUnreadTotal()
    {
        // Arrange
        _notifications
            .Setup(a => a.GetNotificationsAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Notice(12, readAt: null), Notice(11, readAt: ReadTime)]);

        // Act
        var result = await CreateManager().GetNotificationsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var list = ResultAssert.Succeeded(result);
        Assert.Equal(1, list.UnreadCount);
        Assert.Equal([12, 11], list.Notices.Select(n => n.Id));
        Assert.Equal("delay.raised", list.Notices[0].EventType);
    }

    [Fact]
    public async Task GetNotificationsAsync_NoProjectAccess_RefusesWithoutReading()
    {
        // Act
        var act = () => CreateManager().GetNotificationsAsync(UserId, OtherProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _notifications.Verify(a => a.GetNotificationsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkReadAsync_RecipientNotice_MarksReadAndReturnsNewTotal()
    {
        // Arrange
        _notifications
            .Setup(a => a.GetNotificationAsync(UserId, ProjectId, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Notice(12, readAt: ReadTime));
        _notifications.Setup(a => a.CountUnreadAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        // Act
        var result = await CreateManager().MarkReadAsync(UserId, ProjectId, 12, CancellationToken.None);

        // Assert
        var read = ResultAssert.Succeeded(result);
        Assert.Equal((ReadTime, 4), (read.Notice.ReadAt, read.UnreadCount));
        _notifications.Verify(a => a.MarkReadAsync(
            UserId, ProjectId, 12, It.Is<DateTimeOffset>(t => t.Offset == TimeSpan.Zero), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkReadAsync_NotRecipientNotice_ReturnsNotFound()
    {
        // Act
        var result = await CreateManager().MarkReadAsync(UserId, ProjectId, 99, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task MarkReadAsync_NoProjectAccess_RefusesWithoutWriting()
    {
        // Act
        var act = () => CreateManager().MarkReadAsync(UserId, OtherProjectId, 12, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _notifications.Verify(a => a.MarkReadAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Private Methods

    private static NotificationRecord Notice(int id, DateTimeOffset? readAt)
    {
        return new NotificationRecord(id, 1, "FN-1042", "Primer not cured", "delay.raised",
            new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero), readAt);
    }

    private NotificationManager CreateManager()
    {
        return new ManagerTestHost().With(_projectAccess).With(_notifications).Create<NotificationManager>();
    }

    #endregion
}
