using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Projects;

public class ProjectAccessor(FieldNotebookContext context) : IProjectAccessor
{
    public async Task<ProjectAccessRecord?> GetProjectAccessAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await (
            from up in context.UserProjects.AsNoTracking()
            join u in context.Users.AsNoTracking() on up.UserId equals u.Id
            join p in context.Projects.AsNoTracking() on up.ProjectId equals p.Id
            join ur in context.UserRoles.AsNoTracking() on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where up.UserId == userId && up.ProjectId == projectId
                  && up.IsActive && u.IsActive && p.IsActive && ur.IsActive && r.IsActive
            select new { ProjectName = p.Name, RoleName = r.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (access is null)
        {
            return null;
        }

        var crews = await context.Crew.AsNoTracking()
            .Where(c => c.ProjectId == projectId && c.IsActive
                        && (c.ForemanLeadId == userId || c.SuperintendentUserId == userId))
            .OrderBy(c => c.Name)
            .Select(c => new CrewLeadRecord(c.Id, c.ProjectId, c.Name, c.ForemanLeadId == userId))
            .ToListAsync(cancellationToken);

        return new ProjectAccessRecord(projectId, access.ProjectName, access.RoleName, crews);
    }

    public async Task<IReadOnlyList<ReportCrewRecord>> GetCrewsAsync(int projectId, CancellationToken cancellationToken) =>
        await context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive)
            .OrderBy(c => c.Name).Select(c => new ReportCrewRecord(c.Id, c.Name, c.ForemanLeadId, c.SuperintendentUserId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportSegmentRecord>> GetSegmentsAsync(int projectId, CancellationToken cancellationToken) =>
        await context.Segments.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActive)
            .OrderBy(s => s.Name).Select(s => new ReportSegmentRecord(s.Id, s.Name)).ToListAsync(cancellationToken);

    public Task<ReportMapRecord?> GetActiveSiteMapAsync(int projectId, CancellationToken cancellationToken) =>
        context.SiteMaps.AsNoTracking().Where(m => m.ProjectId == projectId && m.IsActive)
            .OrderBy(m => m.Id).Select(m => new ReportMapRecord(m.Id, m.Name, m.Uri)).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportPersonRecord>> GetMembersAsync(int projectId, CancellationToken cancellationToken)
    {
        var people = await (from up in context.UserProjects.AsNoTracking()
            join u in context.Users.AsNoTracking() on up.UserId equals u.Id
            join ur in context.UserRoles.AsNoTracking() on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where up.ProjectId == projectId && up.IsActive && u.IsActive && ur.IsActive && r.IsActive
                && (r.Name == RoleNames.Foreman || r.Name == RoleNames.Superintendent)
            select new { u.Id, u.DisplayName, RoleName = r.Name }).ToListAsync(cancellationToken);
        var crews = await context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive)
            .Select(c => new { c.Id, c.Name, c.ForemanLeadId, c.SuperintendentUserId }).ToListAsync(cancellationToken);
        return (from p in people from c in crews
            where p.Id == (p.RoleName == RoleNames.Foreman ? c.ForemanLeadId : c.SuperintendentUserId)
            select new ReportPersonRecord(p.Id, p.DisplayName, c.Id, c.Name, p.RoleName)).ToList();
    }

    public async Task<IReadOnlyList<CrewContactRecord>> GetCrewContactsAsync(int projectId, CancellationToken cancellationToken)
    {
        var crews = context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive);
        var contacts = crews.Select(c => new { CrewId = c.Id, UserId = c.ForemanLeadId })
            .Union(crews.Select(c => new { CrewId = c.Id, UserId = c.SuperintendentUserId }));

        return await (
            from contact in contacts
            join up in context.UserProjects.AsNoTracking() on contact.UserId equals up.UserId
            join u in context.Users.AsNoTracking() on up.UserId equals u.Id
            where up.ProjectId == projectId && up.IsActive && u.IsActive
            orderby contact.CrewId, contact.UserId
            select new CrewContactRecord(contact.CrewId, contact.UserId))
            .ToListAsync(cancellationToken);
    }
}
