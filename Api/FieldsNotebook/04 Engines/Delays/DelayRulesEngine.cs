using Common.Delays;

namespace Engines.Delays
{
    /// <summary>Shared delay rules from the approved workflow.</summary>
    public static class DelayRulesEngine
    {
        /// <summary>A reopened delay stays High priority until its reopened cycle closes.</summary>
        public static bool IsHighPriority(int cycleCount, bool hasOpenCycle) => hasOpenCycle && cycleCount > 1;

        public static bool IsAdvanceNotice(string reportType) => reportType == DelayReportTypes.AdvanceNotice;

        /// <summary>The delay waits on a handover check by one of the given affected crews in the current cycle.</summary>
        public static bool AwaitsHandoverCheck(string statusCode, IEnumerable<ActiveDelayImpactRecord> impacts, IReadOnlySet<int> crewIds) =>
            statusCode == DelayStatusCodes.ReadyForHandover
            && impacts.Any(i => crewIds.Contains(i.BlockedCrewId) && !i.IsVerifiedInOpenCycle);
    }
}
