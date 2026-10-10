using Common.Roles;
using Common.Users;

namespace Engines.Crews
{
    /// <summary>Which crews a project role speaks for.</summary>
    public static class CrewCoverageEngine
    {
        /// <summary>A Foreman covers the crew they lead; a Superintendent covers the crews they direct.</summary>
        public static IReadOnlyList<CrewLeadRecord> GetCoveredCrews(string roleName, IEnumerable<CrewLeadRecord> crews)
        {
            var isForeman = roleName == RoleNames.Foreman;
            return crews.Where(c => c.IsForemanLead == isForeman).ToList();
        }

        /// <summary>A Foreman of a crew in the project can raise a delay for the affected or responsible crew.</summary>
        public static bool CanReportDelay(string roleName, int coveredCrewCount) =>
            roleName == RoleNames.Foreman && coveredCrewCount > 0;
    }
}
