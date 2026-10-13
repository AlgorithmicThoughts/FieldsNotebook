using DataContract.Domain.Users;
using DataContract.Me;
using DataContract.Users;

namespace DataContract.Mapping.DomainToClientMapping;

public static class UserDomainToClientMapping
{
    public static UserResponse ToResponse(this UserRecord user)
    {
        return new UserResponse(user.Id, user.DisplayName, user.Email, user.Phone, user.IsActive);
    }

    public static UserSettingsResponse ToResponse(this UserSettingsRecord settings)
    {
        return new UserSettingsResponse(settings.ThemePreference, settings.EmailEnabled, settings.SmsEnabled);
    }

    /// <summary>A user with no UserSettings row reads the defaults.</summary>
    public static CurrentUserResponse ToResponse(this CurrentUserRecord user, IEnumerable<CurrentUserProjectResponse>? projects)
    {
        var settings = user.Settings ?? UserSettingsDefaults.Value;
        return new CurrentUserResponse(user.Id, user.DisplayName, user.Email, user.Phone, settings.ThemePreference,
            settings.EmailEnabled, settings.SmsEnabled, projects.MapAll(p => p));
    }

    public static CurrentUserProjectResponse ToResponse(this UserProjectRecord project, IEnumerable<CrewLeadRecord>? coveredCrews,
        bool canReportDelay)
    {
        return new CurrentUserProjectResponse(project.ProjectId, project.Name, project.Code, project.DistrictName,
            project.RoleName, coveredCrews.MapAll(ToResponse), canReportDelay);
    }

    public static CrewResponse ToResponse(this CrewLeadRecord crew)
    {
        return new CrewResponse(crew.CrewId, crew.Name);
    }
}
