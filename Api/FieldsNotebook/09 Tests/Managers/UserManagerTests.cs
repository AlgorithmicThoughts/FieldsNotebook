using Accessors;
using Accessors.Users;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Users;
using DataContract.Me;
using Engines;
using Engines.Crews;
using Managers.Users;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Managers;

public class UserManagerTests
{
    private const int UserId = 3;

    [Fact]
    public async Task GetUserAsync_Themselves_ReturnsUser()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        mocks.UserAccessor.Setup(a => a.GetUserAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(7, "Dana Foreman", "dana@example.com", "+15555550100", true));

        // Act
        var result = await CreateMockManager(mocks).GetUserAsync(7, 7, CancellationToken.None);

        // Assert
        var user = ResultAssert.Succeeded(result);
        Assert.Equal((7, "Dana Foreman", "dana@example.com"), (user.Id, user.DisplayName, user.Email));
        mocks.UserAccessor.Verify(a => a.SharesActiveProjectAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserAsync_SomeoneOnASharedProject_ReturnsUser()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpUser(mocks, isActive: true);
        mocks.UserAccessor.Setup(a => a.GetUserAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(7, "Dana Foreman", "dana@example.com", "+15555550100", true));
        mocks.UserAccessor.Setup(a => a.SharesActiveProjectAsync(UserId, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await CreateMockManager(mocks).GetUserAsync(UserId, 7, CancellationToken.None);

        // Assert
        Assert.Equal(7, ResultAssert.Succeeded(result).Id);
    }

    [Fact]
    public async Task GetUserAsync_SomeoneOnNoSharedProject_ReadsAsMissingWithoutReadingThem()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpUser(mocks, isActive: true);

        // Act
        var result = await CreateMockManager(mocks).GetUserAsync(UserId, 7, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        mocks.UserAccessor.Verify(a => a.GetUserAsync(7, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserAsync_InactiveRequester_ReadsAsMissing()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpUser(mocks, isActive: false);
        mocks.UserAccessor.Setup(a => a.SharesActiveProjectAsync(UserId, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await CreateMockManager(mocks).GetUserAsync(UserId, 7, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task GetUserAsync_MissingUser_ReturnsNotFound()
    {
        // Arrange
        var mocks = new UserManagerMocks();

        // Act
        var result = await CreateMockManager(mocks).GetUserAsync(99, 99, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_Foreman_ReturnsLedCrewAndCanReport()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpCurrentUser(mocks,
            [new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", RoleNames.Foreman)],
            [new CrewLeadRecord(3, 1, "Welding and Erection", IsForemanLead: true)]);

        // Act
        var result = await CreateMockManager(mocks).GetCurrentUserAsync(UserId, CancellationToken.None);

        // Assert
        var project = Assert.Single(ResultAssert.Succeeded(result).Projects);
        Assert.Equal([new CrewResponse(3, "Welding and Erection")], project.Crews);
        Assert.True(project.CanReportDelay);
    }

    [Fact]
    public async Task GetCurrentUserAsync_Superintendent_ReturnsDirectedCrewsOfThatProjectAndCannotReport()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpCurrentUser(mocks,
            [new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", RoleNames.Superintendent)],
            [new CrewLeadRecord(4, 1, "Coatings", IsForemanLead: false),
             new CrewLeadRecord(3, 1, "Welding and Erection", IsForemanLead: false),
             new CrewLeadRecord(9, 3, "Other project crew", IsForemanLead: false)]);

        // Act
        var result = await CreateMockManager(mocks).GetCurrentUserAsync(UserId, CancellationToken.None);

        // Assert
        var project = Assert.Single(ResultAssert.Succeeded(result).Projects);
        Assert.Equal(["Coatings", "Welding and Erection"], project.Crews.Select(c => c.Name));
        Assert.False(project.CanReportDelay);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NoSettingsRow_ReturnsDefaults()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpCurrentUser(mocks, [], []);

        // Act
        var result = await CreateMockManager(mocks).GetCurrentUserAsync(UserId, CancellationToken.None);

        // Assert
        var user = ResultAssert.Succeeded(result);
        Assert.Equal(("System", true, false), (user.ThemePreference, user.EmailEnabled, user.SmsEnabled));
    }

    [Fact]
    public async Task GetCurrentUserAsync_UnknownUser_ReturnsNotFound()
    {
        // Arrange
        var mocks = new UserManagerMocks();

        // Act
        var result = await CreateMockManager(mocks).GetCurrentUserAsync(99, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ChangedTheme_SavesOnlyThatColumnAndReturnsSavedRow()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpUser(mocks, isActive: true);
        UserSettingsChangeRecord? sent = null;
        mocks.UserAccessor
            .Setup(a => a.SaveSettingsAsync(UserId, It.IsAny<UserSettingsChangeRecord>(), It.IsAny<CancellationToken>()))
            .Callback<int, UserSettingsChangeRecord, CancellationToken>((_, change, _) => sent = change)
            .ReturnsAsync(new UserSettingsRecord("Dark", true, false));

        // Act
        var result = await CreateMockManager(mocks).UpdateSettingsAsync(UserId, new UserSettingsRequest("Dark", null, null), CancellationToken.None);

        // Assert
        Assert.Equal(new UserSettingsResponse("Dark", true, false), ResultAssert.Succeeded(result));
        Assert.Equal(new UserSettingsChangeRecord("Dark", null, null), sent);
    }

    [Fact]
    public async Task UpdateSettingsAsync_InactiveUser_ReturnsNotFoundWithoutSaving()
    {
        // Arrange
        var mocks = new UserManagerMocks();
        SetUpUser(mocks, isActive: false);

        // Act
        var result = await CreateMockManager(mocks).UpdateSettingsAsync(UserId, new UserSettingsRequest(null, false, null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        mocks.UserAccessor.Verify(
            a => a.SaveSettingsAsync(It.IsAny<int>(), It.IsAny<UserSettingsChangeRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #region Private Test Helper

    /// <summary>A UserManager over the real Engine it decides with and the mocked Accessor it needs; it uses no Utility.</summary>
    private static UserManager CreateMockManager(UserManagerMocks mocks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AccessorFactory>();
        services.AddSingleton<EngineFactory>();
        services.AddSingleton<ICrewCoverageEngine, CrewCoverageEngine>();
        mocks.RegistryOverride(services);

        return ActivatorUtilities.CreateInstance<UserManager>(services.BuildServiceProvider());
    }

    private static void SetUpUser(UserManagerMocks mocks, bool isActive)
    {
        mocks.UserAccessor.Setup(a => a.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(UserId, "Dana Foreman", "dana@example.com", null, isActive));
    }

    private static void SetUpCurrentUser(UserManagerMocks mocks, IReadOnlyList<UserProjectRecord> projects, IReadOnlyList<CrewLeadRecord> crews)
    {
        mocks.UserAccessor
            .Setup(a => a.GetCurrentUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrentUserRecord(UserId, "Demo User", "demo@example.com", null, Settings: null, projects, crews));
    }

    /// <summary>The Accessors UserManager resolves, each a mock the test arranges for itself.</summary>
    private sealed class UserManagerMocks
    {
        public Mock<IUserAccessor> UserAccessor { get; } = new();

        /// <summary>Registers each mock in place of the concrete class the factories would otherwise resolve.</summary>
        public void RegistryOverride(IServiceCollection services)
        {
            services.AddSingleton(UserAccessor.Object);
        }
    }

    #endregion
}
