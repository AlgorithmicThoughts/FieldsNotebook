using Accessors.Activity;
using Accessors.Delays;
using Accessors.Projects;
using Common.Activity;
using Common.Delays;
using Common.Errors;
using DataContract.Home;
using Engines.Crews;
using Engines.Delays;

namespace Managers.Home
{
    public class HomeManager(
        IProjectAccessAccessor projectAccessAccessor,
        IDelayAccessor delayAccessor,
        IActivityAccessor activityAccessor) : IHomeManager
    {
        /// <summary>Home shows only the latest handovers; a full timeline belongs to the delay or activity screens.</summary>
        public const int RecentActivityLimit = 5;

        /// <summary>Rows in "Needs attention"; the metrics still count every active delay, and Tracking lists the rest.</summary>
        public const int AttentionLimit = 4;

        public async Task<HomeResponse> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);

            var access = await projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken)
                ?? throw new ProjectAccessDeniedException(projectId);
            var ourCrewIds = CrewCoverageEngine.GetCoveredCrews(access.RoleName, access.Crews)
                .Select(c => c.CrewId)
                .ToHashSet();
            if (ourCrewIds.Count == 0)
            {
                return BuildHome(new ActiveDelayView(ActiveDelaySet.Empty, ourCrewIds), new ProjectRef(projectId, access.ProjectName), []);
            }

            // Sequential on purpose: both Accessors share this request's DbContext.
            var active = await delayAccessor.GetActiveDelaysForCrewsAsync(projectId, ourCrewIds, cancellationToken);
            var activity = await activityAccessor.GetRecentActivityForCrewsAsync(projectId, ourCrewIds, RecentActivityLimit, cancellationToken);

            return BuildHome(new ActiveDelayView(active, ourCrewIds), new ProjectRef(projectId, access.ProjectName), activity);
        }

        private static HomeResponse BuildHome(ActiveDelayView view, ProjectRef project, IReadOnlyList<ActivityEventRecord> activity) =>
            new(
                BuildMetrics(view),
                view.Delays
                    .Select(d => ToSummary(d, view, project))
                    .OrderByDescending(d => d.IsHighPriority)
                    .Take(AttentionLimit)
                    .ToList(),
                BuildAdvanceNotice(view, project),
                activity.Select(ToActivityEntry).ToList());

        private sealed record ProjectRef(int Id, string Name);

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
        private static DelaySummaryResponse? BuildAdvanceNotice(ActiveDelayView view, ProjectRef project) =>
            view.Delays
                .Where(d => DelayRulesEngine.IsAdvanceNotice(d.ReportType) && view.BlocksOurCrews(d))
                .OrderBy(d => d.ExpectedReadyAt is null)
                .ThenBy(d => d.ExpectedReadyAt)
                .Select(d => ToSummary(d, view, project))
                .FirstOrDefault();

        private static DelaySummaryResponse ToSummary(ActiveDelayRecord delay, ActiveDelayView view, ProjectRef project) =>
            new(
                delay.DelayId,
                delay.Number,
                delay.Title,
                delay.StatusCode,
                delay.TagCode,
                delay.TagName,
                delay.OwnerCrewName,
                view.BlockedCrewNamesOf(delay),
                view.ImpactsOf(delay).Select(i => new DelayImpactResponse(i.ImpactId, i.BlockedCrewName, i.BlockedWorkItemId)).ToList(),
                delay.LocationName,
                project.Id,
                project.Name,
                DelayRulesEngine.IsHighPriority(delay.CycleCount, delay.HasOpenCycle),
                delay.ReportType,
                delay.CreatedAt,
                delay.ExpectedReadyAt,
                delay.PinX is { } x && delay.PinY is { } y ? new DelayPinResponse(x, y) : null,
                delay.SiteMapId,
                delay.SegmentName);

        private static ActivityEntryResponse ToActivityEntry(ActivityEventRecord e) =>
            new(e.Id, e.EventType, e.DelayNumber, e.DelayTitle, e.ActorName, e.FromStatusCode, e.ToStatusCode,
                e.NewExpectedReadyAt, e.AffectedCrewName, e.CreatedAt);

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
