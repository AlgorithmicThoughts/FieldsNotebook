using Common.Delays;
using Common.Projects;
using Common.Roles;

namespace Engines.Delays
{
    /// <summary>
    /// Who may move a delay and when, from the approved workflow. Every decision here is shared
    /// between the workflow actions; none of it reaches the database or the HTTP contract.
    /// </summary>
    public static class DelayWorkflowEngine
    {
        private static readonly string[] ActiveStatusCodes =
            [DelayStatusCodes.Raised, DelayStatusCodes.Acknowledged, DelayStatusCodes.ReadyForHandover, DelayStatusCodes.Reopened];

        /// <summary>The delay is neither resolved nor revoked, so the workflow can still move it.</summary>
        public static bool IsActive(string statusCode) => ActiveStatusCodes.Contains(statusCode);

        /// <summary>The user is the Superintendent who directs this crew.</summary>
        public static bool DirectsCrew(ProjectAccessRecord access, int crewId) =>
            access.RoleName == RoleNames.Superintendent
            && access.Crews.Any(c => c.CrewId == crewId && !c.IsForemanLead);

        /// <summary>The user is the Foreman who leads this crew.</summary>
        public static bool LeadsCrew(ProjectAccessRecord access, int crewId) =>
            access.RoleName == RoleNames.Foreman
            && access.Crews.Any(c => c.CrewId == crewId && c.IsForemanLead);

        /// <summary>Any Superintendent with access to the project reopens a resolved delay.</summary>
        public static bool IsSuperintendent(ProjectAccessRecord access) =>
            access.RoleName == RoleNames.Superintendent;

        /// <summary>Every affected crew verified the handover for the current cycle.</summary>
        public static bool HasEveryHandoverCheck(DelayWorkflowState state) =>
            state.Impacts.Count > 0 && state.Impacts.All(i => i.IsVerifiedInCycle);

        /// <summary>A reopened delay needs proof-of-work photos in its reopened cycle before closure.</summary>
        public static bool RequiresProofOfWork(DelayWorkflowState state) => state.CycleCount > 1;
    }
}
