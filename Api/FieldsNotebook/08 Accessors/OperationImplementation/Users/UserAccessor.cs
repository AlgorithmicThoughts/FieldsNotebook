using Common.Users;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Users
{
    public class UserAccessor(FieldNotebookContext context) : IUserAccessor
    {
        public Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken) =>
            context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserRecord(u.Id, u.DisplayName, u.Email, u.Phone, u.IsActive))
                .SingleOrDefaultAsync(cancellationToken);

        public async Task<CurrentUserRecord?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
        {
            var user = await (
                from u in context.Users.AsNoTracking()
                join s in context.UserSettings.AsNoTracking() on u.Id equals s.UserId into settings
                from s in settings.DefaultIfEmpty()
                where u.Id == userId && u.IsActive
                select new
                {
                    u.DisplayName,
                    u.Email,
                    u.Phone,
                    Settings = s == null ? null : new UserSettingsRecord(s.ThemePreference, s.EmailEnabled, s.SmsEnabled),
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (user is null)
            {
                return null;
            }

            var projects = await (
                from up in context.UserProjects.AsNoTracking()
                join p in context.Projects.AsNoTracking() on up.ProjectId equals p.Id
                join d in context.Districts.AsNoTracking() on p.DistrictId equals d.Id
                join ur in context.UserRoles.AsNoTracking() on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
                join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where up.UserId == userId && up.IsActive && p.IsActive && ur.IsActive && r.IsActive
                orderby p.Name
                select new UserProjectRecord(p.Id, p.Name, p.Code, d.Name, r.Name))
                .ToListAsync(cancellationToken);

            var crews = await context.Crew.AsNoTracking()
                .Where(c => c.IsActive && (c.ForemanLeadId == userId || c.SuperintendentUserId == userId))
                .OrderBy(c => c.Name)
                .Select(c => new CrewLeadRecord(c.Id, c.ProjectId, c.Name, c.ForemanLeadId == userId))
                .ToListAsync(cancellationToken);

            return new CurrentUserRecord(userId, user.DisplayName, user.Email, user.Phone, user.Settings, projects, crews);
        }
    }
}
