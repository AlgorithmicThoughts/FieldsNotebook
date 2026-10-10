using Common.Users;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Users
{
    public class UserAccessor(FieldNotebookContext context) : IUserAccessor
    {
        public Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken) =>
            context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserRecord(u.Id, u.DisplayName, u.Email, u.Phone, u.IsActive))
                .SingleOrDefaultAsync(cancellationToken);

        public async Task<CurrentUserRecord?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
        {
            var displayName = await context.Users
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => u.DisplayName)
                .SingleOrDefaultAsync(cancellationToken);
            if (displayName is null)
            {
                return null;
            }

            var projects = await (
                from up in context.UserProjects
                join p in context.Projects on up.ProjectId equals p.Id
                join d in context.Districts on p.DistrictId equals d.Id
                join ur in context.UserRoles on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
                join r in context.Roles on ur.RoleId equals r.Id
                where up.UserId == userId && up.IsActive && p.IsActive && ur.IsActive && r.IsActive
                orderby p.Name
                select new UserProjectRecord(p.Id, p.Name, p.Code, d.Name, r.Name))
                .ToListAsync(cancellationToken);

            var crews = await context.Crew
                .Where(c => c.IsActive && (c.ForemanLeadId == userId || c.SuperintendentUserId == userId))
                .OrderBy(c => c.Name)
                .Select(c => new CrewLeadRecord(c.ProjectId, c.Name, c.ForemanLeadId == userId))
                .ToListAsync(cancellationToken);

            return new CurrentUserRecord(userId, displayName, projects, crews);
        }
    }
}
