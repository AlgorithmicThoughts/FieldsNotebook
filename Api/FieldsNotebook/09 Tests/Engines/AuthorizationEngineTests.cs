using Accessors;
using Accessors.Projects;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Projects;
using Engines.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Engines;

public class AuthorizationEngineTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;

    [Fact]
    public async Task AuthorizeProjectAsync_UserWithoutProjectAccess_IsForbidden()
    {
        // Arrange
        var mocks = new AuthorizationEngineMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);
        var engine = CreateMockEngine(mocks);

        // Act
        var act = () => engine.AuthorizeProjectAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        mocks.Logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains($"{UserId}") && state.ToString()!.Contains($"{ProjectId}")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task AuthorizeProjectAsync_UserWithProjectAccess_ReturnsTheAccess()
    {
        // Arrange
        var mocks = new AuthorizationEngineMocks();
        var access = new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []);
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(access);
        var engine = CreateMockEngine(mocks);

        // Act
        var authorized = await engine.AuthorizeProjectAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Same(access, authorized);
    }

    #region Private Test Helper

    /// <summary>An AuthorizationEngine over the mocked Accessor and Utility it needs.</summary>
    private static AuthorizationEngine CreateMockEngine(AuthorizationEngineMocks mocks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AccessorFactory>();
        mocks.RegistryOverride(services);

        return ActivatorUtilities.CreateInstance<AuthorizationEngine>(services.BuildServiceProvider());
    }

    /// <summary>The Accessors and Utilities AuthorizationEngine resolves, each a mock the test arranges for itself.</summary>
    private sealed class AuthorizationEngineMocks
    {
        public Mock<IProjectAccessor> ProjectAccessor { get; } = new();
        public Mock<ILogger<AuthorizationEngine>> Logger { get; } = new();

        /// <summary>Registers each mock in place of the concrete class the factories would otherwise resolve.</summary>
        public void RegistryOverride(IServiceCollection services)
        {
            services.AddSingleton(ProjectAccessor.Object);
            services.AddSingleton(Logger.Object);
        }
    }

    #endregion
}
