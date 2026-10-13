using DataContract.Domain.Users;
using DataContract.Me;

namespace DataContract.Mapping.ClientToDomainMapping;

public static class UserClientToDomainMapping
{
    public static UserSettingsChangeRecord ToDomain(this UserSettingsRequest request)
    {
        return new UserSettingsChangeRecord(request.ThemePreference, request.EmailEnabled, request.SmsEnabled);
    }
}
