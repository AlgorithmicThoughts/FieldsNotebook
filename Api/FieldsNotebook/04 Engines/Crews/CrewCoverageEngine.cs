using Common.Roles;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Me;

namespace Engines.Crews
{
    /// <summary>Which crews a project role speaks for.</summary>
    public static class CrewCoverageEngine
    {
        /// <summary>The crews the user's role covers in the project, by ID.</summary>
        public static IReadOnlySet<int> GetCoveredCrewIds(ProjectAccessRecord access)
        {
            return GetCoveredCrews(access.RoleName, access.Crews).Select(c => c.CrewId).ToHashSet();
        }

        /// <summary>Each of the user's projects with the crews their role covers there and whether they can report a delay.</summary>
        public static IReadOnlyList<CurrentUserProjectResponse> BuildProjects(CurrentUserRecord user)
        {
            return user.Projects.Select(project =>
            {
                var coveredCrews = GetCoveredCrews(project.RoleName, user.Crews.Where(c => c.ProjectId == project.ProjectId));
                return project.ToResponse(coveredCrews, CanReportDelay(project.RoleName, coveredCrews.Count));
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
}
