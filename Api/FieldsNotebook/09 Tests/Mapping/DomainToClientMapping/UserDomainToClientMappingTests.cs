using DataContract.Domain.Users;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Me;

namespace Tests.Mapping.DomainToClientMapping;

public class UserDomainToClientMappingTests
{
    [Fact]
    public void ToResponse_User_KeepsContactDetailsAndActiveFlag()
    {
        // Arrange
        var user = new UserRecord(7, "Dana Foreman", "dana@example.com", "+15555550100", false);

        // Act
        var response = user.ToResponse();

        // Assert
        Assert.Equal((7, "Dana Foreman", "dana@example.com", "+15555550100", false),
            (response.Id, response.DisplayName, response.Email, response.Phone, response.IsActive));
    }

    [Fact]
    public void ToResponse_CurrentUserWithSettings_UsesTheSavedSettingsAndProjects()
    {
        // Arrange
        var user = CurrentUser(new UserSettingsRecord("Dark", false, true));
        List<CurrentUserProjectResponse> projects = [new(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", "Foreman", [], true)];

        // Act
        var response = user.ToResponse(projects);

        // Assert
        Assert.Equal(("Dark", false, true), (response.ThemePreference, response.EmailEnabled, response.SmsEnabled));
        Assert.Equal("OMA-FAB", Assert.Single(response.Projects).Code);
    }

    [Fact]
    public void ToResponse_CurrentUserWithoutSettings_UsesTheDefaults()
    {
        // Arrange
        var user = CurrentUser(settings: null);

        // Act
        var response = user.ToResponse([]);

        // Assert
        Assert.Equal(("System", true, false), (response.ThemePreference, response.EmailEnabled, response.SmsEnabled));
    }

    [Fact]
    public void ToResponse_CurrentUserWithNullProjects_MapsToAnEmptyList()
    {
        // Arrange
        var user = CurrentUser(settings: null);

        // Act
        var response = user.ToResponse(null);

        // Assert
        Assert.Empty(response.Projects);
    }

    [Fact]
    public void ToResponse_CurrentUserWithEmptyProjects_MapsToAnEmptyList()
    {
        // Arrange
        var user = CurrentUser(settings: null);

        // Act
        var response = user.ToResponse([]);

        // Assert
        Assert.Empty(response.Projects);
    }

    [Fact]
    public void ToResponse_Project_MapsTheCoveredCrews()
    {
        // Arrange
        var project = Project();
        List<CrewLeadRecord> crews = [new(3, 1, "Welding and Erection", true)];

        // Act
        var response = project.ToResponse(crews, canReportDelay: true);

        // Assert
        Assert.Equal([new CrewResponse(3, "Welding and Erection")], response.Crews);
        Assert.True(response.CanReportDelay);
    }

    [Fact]
    public void ToResponse_ProjectWithNullCrews_MapsToAnEmptyList()
    {
        // Arrange
        var project = Project();

        // Act
        var response = project.ToResponse(null, canReportDelay: false);

        // Assert
        Assert.Empty(response.Crews);
    }

    [Fact]
    public void ToResponse_ProjectWithEmptyCrews_MapsToAnEmptyList()
    {
        // Arrange
        var project = Project();

        // Act
        var response = project.ToResponse([], canReportDelay: false);

        // Assert
        Assert.Empty(response.Crews);
    }

    [Fact]
    public void ToResponse_SavedSettings_MapsEachPreference()
    {
        // Arrange
        var settings = new UserSettingsRecord("Light", true, true);

        // Act
        var response = settings.ToResponse();

        // Assert
        Assert.Equal(new UserSettingsResponse("Light", true, true), response);
    }

    private static CurrentUserRecord CurrentUser(UserSettingsRecord? settings)
    {
        return new CurrentUserRecord(3, "Demo User", "demo@example.com", null, settings, [], []);
    }

    private static UserProjectRecord Project()
    {
        return new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", "Foreman");
    }
}
