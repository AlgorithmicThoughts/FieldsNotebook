using Accessors.Users;
using Common.Errors;
using Common.Users;
using DataContract.Me;
using DataContract.Users;
using Engines.Crews;

namespace Managers.Users
{
    public class UserManager(IUserAccessor userAccessor) : IUserManager
    {
        public async Task<UserResponse?> GetUserAsync(int userId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);

            var user = await userAccessor.GetUserAsync(userId, cancellationToken);
            return user is null
                ? null
                : new UserResponse(user.Id, user.DisplayName, user.Email, user.Phone, user.IsActive);
        }

        public async Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);

            var user = await userAccessor.GetCurrentUserAsync(userId, cancellationToken);
            if (user is null)
            {
                return null;
            }

            var settings = user.Settings ?? UserSettingsDefaults.Value;
            return new CurrentUserResponse(
                user.Id,
                user.DisplayName,
                user.Email,
                user.Phone,
                settings.ThemePreference,
                settings.EmailEnabled,
                settings.SmsEnabled,
                user.Projects.Select(p => ToProjectResponse(p, user.Crews)).ToList());
        }

        public async Task<UserSettingsResponse?> UpdateSettingsAsync(int userId, UserSettingsRequest request, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
            var change = ToSettingsChange(request);

            var user = await userAccessor.GetUserAsync(userId, cancellationToken);
            if (user is not { IsActive: true })
            {
                return null;
            }

            var saved = await userAccessor.SaveSettingsAsync(userId, change, cancellationToken);
            return new UserSettingsResponse(saved.ThemePreference, saved.EmailEnabled, saved.SmsEnabled);
        }

        private static UserSettingsChangeRecord ToSettingsChange(UserSettingsRequest request)
        {
            if (request is { ThemePreference: null, EmailEnabled: null, SmsEnabled: null })
            {
                throw new InvalidUserSettingsException("Choose a setting to change.");
            }

            if (request.ThemePreference is { } theme && !ThemePreferences.IsKnown(theme))
            {
                throw new InvalidUserSettingsException("Appearance must be System, Light, or Dark.");
            }

            return new UserSettingsChangeRecord(request.ThemePreference, request.EmailEnabled, request.SmsEnabled);
        }

        private static CurrentUserProjectResponse ToProjectResponse(UserProjectRecord project, IReadOnlyList<CrewLeadRecord> crews)
        {
            var coveredCrews = CrewCoverageEngine
                .GetCoveredCrews(project.RoleName, crews.Where(c => c.ProjectId == project.ProjectId))
                .Select(c => new CrewResponse(c.CrewId, c.Name))
                .ToList();

            return new CurrentUserProjectResponse(
                project.ProjectId,
                project.Name,
                project.Code,
                project.DistrictName,
                project.RoleName,
                coveredCrews,
                CrewCoverageEngine.CanReportDelay(project.RoleName, coveredCrews.Count));
        }
    }
}
