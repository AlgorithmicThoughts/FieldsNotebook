using Accessors;
using Accessors.Projects;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Projects;
using Engines.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Engines;

public class AuthorizationEngineTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;

    private readonly Mock<IProjectAccessAccessor> _projectAccess = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AuthorizeProjectAsync_NoIdentity_IsUnauthorizedWithoutReadingAccess(int userId)
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var act = () => engine.AuthorizeProjectAsync(userId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Unauthorized, act);
        _projectAccess.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AuthorizeProjectAsync_UserWithoutProjectAccess_IsForbidden()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var act = () => engine.AuthorizeProjectAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
    }

    [Fact]
    public async Task AuthorizeProjectAsync_UserWithProjectAccess_ReturnsTheAccess()
    {
        // Arrange
        var access = new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []);
        _projectAccess.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(access);
        var engine = CreateEngine();

        // Act
        var authorized = await engine.AuthorizeProjectAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Same(access, authorized);
    }

    private AuthorizationEngine CreateEngine()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_projectAccess.Object);
        return new AuthorizationEngine(new AccessorFactory(services.BuildServiceProvider()));
    }
}
