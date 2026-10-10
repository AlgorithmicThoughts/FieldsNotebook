using Common.Projects;
using Common.Users;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Projects
{
    public class ProjectAccessAccessor(FieldNotebookContext context) : IProjectAccessAccessor
    {
        public async Task<ProjectAccessRecord?> GetProjectAccessAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            var access = await (
                from up in context.UserProjects
                join u in context.Users on up.UserId equals u.Id
                join p in context.Projects on up.ProjectId equals p.Id
                join ur in context.UserRoles on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
                join r in context.Roles on ur.RoleId equals r.Id
                where up.UserId == userId && up.ProjectId == projectId
                      && up.IsActive && u.IsActive && p.IsActive && ur.IsActive && r.IsActive
                select new { ProjectName = p.Name, RoleName = r.Name })
                .SingleOrDefaultAsync(cancellationToken);
            if (access is null)
            {
                return null;
            }

            var crews = await context.Crew
                .Where(c => c.ProjectId == projectId && c.IsActive
                            && (c.ForemanLeadId == userId || c.SuperintendentUserId == userId))
                .OrderBy(c => c.Name)
                .Select(c => new CrewLeadRecord(c.Id, c.ProjectId, c.Name, c.ForemanLeadId == userId))
                .ToListAsync(cancellationToken);

            return new ProjectAccessRecord(projectId, access.ProjectName, access.RoleName, crews);
        }
    }
}
