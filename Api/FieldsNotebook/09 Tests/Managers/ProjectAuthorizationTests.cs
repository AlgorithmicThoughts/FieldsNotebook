using Accessors;
using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Projects;
using Managers.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Utilities.OperationContract;

namespace Tests.Managers;

/// <summary>The project membership check every Manager runs first, shown through the smallest Manager that uses it.</summary>
public class ProjectAuthorizationTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;

    [Fact]
    public async Task AuthorizeProjectAsync_UserWithoutProjectAccess_IsForbidden()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);

        // Act
        var result = await CreateMockManager(mocks).GetUnreadCountAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.Logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains($"{UserId}") && state.ToString()!.Contains($"{ProjectId}")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task AuthorizeProjectAsync_UserWithProjectAccess_ProceedsWithoutLogging()
    {
        // Arrange
        var mocks = new NotificationManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []));
        mocks.NotificationAccessor.Setup(a => a.CountUnreadAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        var result = await CreateMockManager(mocks).GetUnreadCountAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        ResultAssert.Succeeded(result);
        mocks.Logger.VerifyNoOtherCalls();
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
