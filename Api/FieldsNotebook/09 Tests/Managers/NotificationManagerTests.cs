using Accessors;
using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Notifications;
using DataContract.Domain.Projects;
using Managers.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Utilities.OperationContract;

namespace Tests.Managers;

public class NotificationManagerTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int OtherProjectId = 2;

    [Fact]
    public async Task GetUnreadCountAsync_WithAccess_ReturnsRecipientCount()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        GrantAccess(mocks);
        mocks.NotificationAccessor.Setup(a => a.CountUnreadAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        // Act
        var result = await CreateMockManager(mocks).GetUnreadCountAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Equal(2, ResultAssert.Succeeded(result).UnreadCount);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoProjectAccess_RefusesWithoutCounting()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        GrantAccess(mocks);

        // Act
        var result = await CreateMockManager(mocks).GetUnreadCountAsync(UserId, OtherProjectId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.NotificationAccessor.Verify(a => a.CountUnreadAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithAccess_ReturnsNoticesAndUnreadTotal()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        GrantAccess(mocks);
        mocks.NotificationAccessor
            .Setup(a => a.GetNotificationsAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Notice(12, readAt: null), Notice(11, readAt: new DateTimeOffset(2026, 10, 7, 13, 0, 0, TimeSpan.Zero))]);

        // Act
        var result = await CreateMockManager(mocks).GetNotificationsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var list = ResultAssert.Succeeded(result);
        Assert.Equal(1, list.UnreadCount);
        Assert.Equal([12, 11], list.Notices.Select(n => n.Id));
        Assert.Equal("delay.raised", list.Notices[0].EventType);
    }

    [Fact]
    public async Task GetNotificationsAsync_NoProjectAccess_RefusesWithoutReading()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        GrantAccess(mocks);

        // Act
        var result = await CreateMockManager(mocks).GetNotificationsAsync(UserId, OtherProjectId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.NotificationAccessor.Verify(a => a.GetNotificationsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkReadAsync_RecipientNotice_MarksReadAtTheCurrentTimeAndReturnsNewTotal()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        var readAt = new DateTimeOffset(2026, 10, 7, 13, 0, 0, TimeSpan.Zero);
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(readAt);
        GrantAccess(mocks);
        mocks.NotificationAccessor
            .Setup(a => a.GetNotificationAsync(UserId, ProjectId, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Notice(12, readAt));
        mocks.NotificationAccessor.Setup(a => a.CountUnreadAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        // Act
        var result = await CreateMockManager(mocks).MarkReadAsync(UserId, ProjectId, 12, CancellationToken.None);

        // Assert
        var read = ResultAssert.Succeeded(result);
        Assert.Equal((readAt, 4), (read.Notice.ReadAt, read.UnreadCount));
        mocks.NotificationAccessor.Verify(a => a.MarkReadAsync(UserId, ProjectId, 12, readAt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkReadAsync_NotRecipientNotice_ReturnsNotFound()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        GrantAccess(mocks);
        mocks.NotificationAccessor
            .Setup(a => a.GetNotificationAsync(UserId, ProjectId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationRecord?)null);

        // Act
        var result = await CreateMockManager(mocks).MarkReadAsync(UserId, ProjectId, 99, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task MarkReadAsync_NoProjectAccess_RefusesWithoutWriting()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        GrantAccess(mocks);

        // Act
        var result = await CreateMockManager(mocks).MarkReadAsync(UserId, OtherProjectId, 12, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.NotificationAccessor.Verify(a => a.MarkReadAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Private Test Helper

    /// <summary>A NotificationManager over the mocked Accessors and Utilities it needs; it decides nothing an Engine owns.</summary>
    private static NotificationManager CreateMockManager(NotificationManagerMocks mocks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AccessorFactory>();
        services.AddSingleton<UtilityFactory>();
        mocks.RegistryOverride(services);

        return ActivatorUtilities.CreateInstance<NotificationManager>(services.BuildServiceProvider());
    }

    /// <summary>The user holds access to <see cref="ProjectId"/> only.</summary>
    private static void GrantAccess(NotificationManagerMocks mocks)
    {
        mocks.ProjectAccessor
            .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []));
    }

    private static NotificationRecord Notice(int id, DateTimeOffset? readAt)
    {
        return new NotificationRecord(id, 1, "FN-1042", "Primer not cured", "delay.raised",
            new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero), readAt);
    }

    /// <summary>The Accessors and Utilities NotificationManager resolves, each a mock the test arranges for itself.</summary>
    private sealed class NotificationManagerMocks
    {
        public Mock<IProjectAccessor> ProjectAccessor { get; } = new();
        public Mock<INotificationAccessor> NotificationAccessor { get; } = new();
        public Mock<IClockUtility> ClockUtility { get; } = new();
        public Mock<ILogger<NotificationManager>> Logger { get; } = new();

        /// <summary>Registers each mock in place of the concrete class the factories would otherwise resolve.</summary>
        public void RegistryOverride(IServiceCollection services)
        {
            services.AddSingleton(ProjectAccessor.Object);
            services.AddSingleton(NotificationAccessor.Object);
            services.AddSingleton(ClockUtility.Object);
            services.AddSingleton(Logger.Object);
        }
    }

    #endregion
}
