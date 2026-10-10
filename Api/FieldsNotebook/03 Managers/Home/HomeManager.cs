using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using DataContract.Home;
using Engines.Crews;
using Engines.Delays;

namespace Managers.Home
{
    public class HomeManager(IProjectAccessAccessor projectAccessAccessor, IDelayAccessor delayAccessor) : IHomeManager
    {
        public async Task<HomeResponse> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);

            var access = await projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken)
                ?? throw new ProjectAccessDeniedException(projectId);
            var ourCrewIds = CrewCoverageEngine.GetCoveredCrews(access.RoleName, access.Crews)
                .Select(c => c.CrewId)
                .ToHashSet();
            var active = ourCrewIds.Count == 0
                ? ActiveDelaySet.Empty
                : await delayAccessor.GetActiveDelaysForCrewsAsync(projectId, ourCrewIds, cancellationToken);

            var view = new ActiveDelayView(active, ourCrewIds);
            return new HomeResponse(
                BuildMetrics(view),
                view.Delays.Select(d => ToSummary(d, view, access.ProjectName)).OrderByDescending(d => d.IsHighPriority).ToList(),
                BuildAdvanceNotice(view));
        }

        private static HomeMetricsResponse BuildMetrics(ActiveDelayView view)
        {
            var blockingOurCrews = view.Delays.Count(view.BlocksOurCrews);
            var crewsWaitingOnUs = view.Delays
                .Where(d => view.OurCrewIds.Contains(d.OwnerCrewId))
                .SelectMany(d => view.ImpactsOf(d).Where(i => i.BlockedCrewId != d.OwnerCrewId))
                .Select(i => i.BlockedCrewId)
                .Distinct()
                .Count();
            var readyToVerify = view.Delays.Count(d =>
                DelayRulesEngine.AwaitsHandoverCheck(d.StatusCode, view.ImpactsOf(d), view.OurCrewIds));

            return new HomeMetricsResponse(blockingOurCrews, crewsWaitingOnUs, readyToVerify);
        }

        // The earliest tentative completion first; a notice without an estimate still shows, after the others.
        private static AdvanceNoticeResponse? BuildAdvanceNotice(ActiveDelayView view) =>
            view.Delays
                .Where(d => DelayRulesEngine.IsAdvanceNotice(d.ReportType) && view.BlocksOurCrews(d))
                .OrderBy(d => d.ExpectedReadyAt is null)
                .ThenBy(d => d.ExpectedReadyAt)
                .Select(d => new AdvanceNoticeResponse(
                    d.Number, d.Title, d.Description, d.OwnerCrewName, view.BlockedCrewNamesOf(d), d.ExpectedReadyAt))
                .FirstOrDefault();

        private static DelaySummaryResponse ToSummary(ActiveDelayRecord delay, ActiveDelayView view, string projectName) =>
            new(
                delay.Number,
                delay.Title,
                delay.StatusCode,
                delay.TagCode,
                delay.TagName,
                delay.OwnerCrewName,
                view.BlockedCrewNamesOf(delay),
                delay.LocationName,
                projectName,
                DelayRulesEngine.IsHighPriority(delay.CycleCount, delay.HasOpenCycle));

        /// <summary>Active delays indexed by delay, from the point of view of the user's crews.</summary>
        private sealed class ActiveDelayView(ActiveDelaySet set, IReadOnlySet<int> ourCrewIds)
        {
            private readonly ILookup<int, ActiveDelayImpactRecord> _impacts = set.Impacts.ToLookup(i => i.DelayId);

            public IReadOnlyList<ActiveDelayRecord> Delays => set.Delays;

            public IReadOnlySet<int> OurCrewIds => ourCrewIds;

            public IEnumerable<ActiveDelayImpactRecord> ImpactsOf(ActiveDelayRecord delay) => _impacts[delay.DelayId];

            public bool BlocksOurCrews(ActiveDelayRecord delay) => ImpactsOf(delay).Any(i => ourCrewIds.Contains(i.BlockedCrewId));

            // One crew can be affected through several work items; list it once.
            public IReadOnlyList<string> BlockedCrewNamesOf(ActiveDelayRecord delay) =>
                ImpactsOf(delay).Select(i => i.BlockedCrewName).Distinct().ToList();
        }
    }
}
