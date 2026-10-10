using Accessors.Users;
using Common.Roles;
using Common.Users;
using DataContract.Me;
using DataContract.Users;

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
            return user is null
                ? null
                : new CurrentUserResponse(user.Id, user.DisplayName, user.Projects.Select(p => ToProjectResponse(p, user.Crews)).ToList());
        }

        // A Foreman's crews are the ones they lead; a Superintendent's are the ones they direct.
        private static CurrentUserProjectResponse ToProjectResponse(UserProjectRecord project, IReadOnlyList<CrewLeadRecord> crews)
        {
            var isForeman = project.RoleName == RoleNames.Foreman;
            var crewNames = crews
                .Where(c => c.ProjectId == project.ProjectId && c.IsForemanLead == isForeman)
                .Select(c => c.Name)
                .ToList();

            return new CurrentUserProjectResponse(
                project.ProjectId,
                project.Name,
                project.Code,
                project.DistrictName,
                project.RoleName,
                crewNames,
                CanReportDelay: isForeman && crewNames.Count > 0);
        }
    }
}
