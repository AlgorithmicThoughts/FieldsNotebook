using DataContract.Domain.Users;
using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Me;

namespace Tests.Mapping.ClientToDomainMapping;

public class UserClientToDomainMappingTests
{
    [Fact]
    public void ToDomain_SettingsRequest_KeepsEachChangedPreference()
    {
        // Arrange
        var request = new UserSettingsRequest("Dark", false, true);

        // Act
        var change = request.ToDomain();

        // Assert
        Assert.Equal(new UserSettingsChangeRecord("Dark", false, true), change);
    }

    [Fact]
    public void ToDomain_UnchangedPreferences_StayNullSoTheSavedValueIsKept()
    {
        // Arrange
        var request = new UserSettingsRequest(null, true, null);

        // Act
        var change = request.ToDomain();

        // Assert
        Assert.Equal(new UserSettingsChangeRecord(null, true, null), change);
    }
}
