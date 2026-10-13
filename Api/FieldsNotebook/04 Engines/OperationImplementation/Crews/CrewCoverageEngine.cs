using Common.Roles;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;

namespace Engines.Crews;

public sealed class CrewCoverageEngine : ICrewCoverageEngine
{
    public IReadOnlySet<int> GetCoveredCrewIds(ProjectAccessRecord access)
    {
        return GetCoveredCrews(access.RoleName, access.Crews).Select(c => c.CrewId).ToHashSet();
    }

    public IReadOnlyList<UserProjectCoverageRecord> BuildProjects(CurrentUserRecord user)
    {
        return user.Projects.Select(project =>
        {
            var coveredCrews = GetCoveredCrews(project.RoleName, user.Crews.Where(c => c.ProjectId == project.ProjectId));
            return new UserProjectCoverageRecord(project, coveredCrews, CanReportDelay(project.RoleName, coveredCrews.Count));
        }).ToList();
    }

    #region Private Methods

    /// <summary>A Foreman covers the crew they lead; a Superintendent covers the crews they direct.</summary>
    private static IReadOnlyList<CrewLeadRecord> GetCoveredCrews(string roleName, IEnumerable<CrewLeadRecord> crews)
    {
        var isForeman = roleName == RoleNames.Foreman;
        return crews.Where(c => c.IsForemanLead == isForeman).ToList();
    }

    /// <summary>A Foreman of a crew in the project can raise a delay for the affected or responsible crew.</summary>
    private static bool CanReportDelay(string roleName, int coveredCrewCount)
    {
        return roleName == RoleNames.Foreman && coveredCrewCount > 0;
    }

    #endregion
}
