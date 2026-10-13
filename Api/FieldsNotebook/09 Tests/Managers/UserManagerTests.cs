using Accessors.Users;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Users;
using DataContract.Me;
using Managers.Users;
using Moq;

namespace Tests.Managers;

public class UserManagerTests
{
    private const int UserId = 3;

    private readonly Mock<IUserAccessor> _userAccessor = new();

    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        _userAccessor.Setup(a => a.GetUserAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(7, "Dana Foreman", "dana@example.com", "+15555550100", true));

        // Act
        var result = await CreateManager().GetUserAsync(7, CancellationToken.None);

        // Assert
        var user = ResultAssert.Succeeded(result);
        Assert.Equal((7, "Dana Foreman", "dana@example.com"), (user.Id, user.DisplayName, user.Email));
    }

    [Fact]
    public async Task GetUserAsync_MissingUser_ReturnsNotFound()
    {
        // Act
        var result = await CreateManager().GetUserAsync(99, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_Foreman_ReturnsLedCrewAndCanReport()
    {
        // Arrange
        SetUpCurrentUser(
            [new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", RoleNames.Foreman)],
            [new CrewLeadRecord(3, 1, "Welding and Erection", IsForemanLead: true)]);

        // Act
        var result = await CreateManager().GetCurrentUserAsync(UserId, CancellationToken.None);

        // Assert
        var project = Assert.Single(ResultAssert.Succeeded(result).Projects);
        Assert.Equal([new CrewResponse(3, "Welding and Erection")], project.Crews);
        Assert.True(project.CanReportDelay);
    }

    [Fact]
    public async Task GetCurrentUserAsync_Superintendent_ReturnsDirectedCrewsOfThatProjectAndCannotReport()
    {
        // Arrange
        SetUpCurrentUser(
            [new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", RoleNames.Superintendent)],
            [new CrewLeadRecord(4, 1, "Coatings", IsForemanLead: false),
             new CrewLeadRecord(3, 1, "Welding and Erection", IsForemanLead: false),
             new CrewLeadRecord(9, 3, "Other project crew", IsForemanLead: false)]);

        // Act
        var result = await CreateManager().GetCurrentUserAsync(UserId, CancellationToken.None);

        // Assert
        var project = Assert.Single(ResultAssert.Succeeded(result).Projects);
        Assert.Equal(["Coatings", "Welding and Erection"], project.Crews.Select(c => c.Name));
        Assert.False(project.CanReportDelay);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NoSettingsRow_ReturnsDefaults()
    {
        // Arrange
        SetUpCurrentUser([], []);

        // Act
        var result = await CreateManager().GetCurrentUserAsync(UserId, CancellationToken.None);

        // Assert
        var user = ResultAssert.Succeeded(result);
        Assert.Equal(("System", true, false), (user.ThemePreference, user.EmailEnabled, user.SmsEnabled));
    }

    [Fact]
    public async Task GetCurrentUserAsync_UnknownUser_ReturnsNotFound()
    {
        // Act
        var result = await CreateManager().GetCurrentUserAsync(99, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ChangedTheme_SavesOnlyThatColumnAndReturnsSavedRow()
    {
        // Arrange
        SetUpUser(isActive: true);
        UserSettingsChangeRecord? sent = null;
        _userAccessor
            .Setup(a => a.SaveSettingsAsync(UserId, It.IsAny<UserSettingsChangeRecord>(), It.IsAny<CancellationToken>()))
            .Callback<int, UserSettingsChangeRecord, CancellationToken>((_, change, _) => sent = change)
            .ReturnsAsync(new UserSettingsRecord("Dark", true, false));

        // Act
        var result = await CreateManager().UpdateSettingsAsync(UserId, new UserSettingsRequest("Dark", null, null), CancellationToken.None);

        // Assert
        Assert.Equal(new UserSettingsResponse("Dark", true, false), ResultAssert.Succeeded(result));
        Assert.Equal(new UserSettingsChangeRecord("Dark", null, null), sent);
    }

    [Fact]
    public async Task UpdateSettingsAsync_InactiveUser_ReturnsNotFoundWithoutSaving()
    {
        // Arrange
        SetUpUser(isActive: false);

        // Act
        var result = await CreateManager().UpdateSettingsAsync(UserId, new UserSettingsRequest(null, false, null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        _userAccessor.Verify(
            a => a.SaveSettingsAsync(It.IsAny<int>(), It.IsAny<UserSettingsChangeRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #region Private Methods

    private void SetUpUser(bool isActive)
    {
        _userAccessor.Setup(a => a.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(UserId, "Dana Foreman", "dana@example.com", null, isActive));
    }

    private void SetUpCurrentUser(IReadOnlyList<UserProjectRecord> projects, IReadOnlyList<CrewLeadRecord> crews)
    {
        _userAccessor
            .Setup(a => a.GetCurrentUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrentUserRecord(UserId, "Demo User", "demo@example.com", null, Settings: null, projects, crews));
    }

    private UserManager CreateManager()
    {
        return new ManagerTestHost().With(_userAccessor).Create<UserManager>();
    }

    #endregion
}
