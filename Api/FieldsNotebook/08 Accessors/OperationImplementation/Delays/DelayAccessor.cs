using Common.Delays;
using Common.Reports;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Delays
{
    public class DelayAccessor(FieldNotebookContext context) : IDelayAccessor
    {
        public async Task<ActiveDelaySet> GetActiveDelaysForCrewsAsync(
            int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken)
        {
            var delays = await (
                from d in context.Delays.AsNoTracking()
                join s in context.DelayStatus.AsNoTracking() on d.DelayStatusId equals s.Id
                join t in context.DelayTags.AsNoTracking() on d.DelayTagId equals t.Id
                join owner in context.Crew.AsNoTracking() on d.OwnerCrewId equals owner.Id
                join segment in context.Segments.AsNoTracking() on d.SegmentId equals segment.Id
                where d.ProjectId == projectId
                      && s.Code != DelayStatusCodes.Resolved && s.Code != DelayStatusCodes.Revoked
                      && (crewIds.Contains(d.OwnerCrewId)
                          || context.DelayImpacts.AsNoTracking().Any(i => i.DelayId == d.Id && crewIds.Contains(i.BlockedCrewId)))
                orderby d.CreatedAt descending, d.Id descending
                select new ActiveDelayRecord(
                    d.Id,
                    d.Number,
                    d.Title,
                    d.Description,
                    s.Code,
                    t.Code,
                    t.Name,
                    d.OwnerCrewId,
                    owner.Name,
                    d.ReportType,
                    d.LocationName ?? segment.Name,
                    segment.Name,
                    d.SiteMapId,
                    d.PinX,
                    d.PinY,
                    d.CreatedAt,
                    context.DelayCycles.AsNoTracking().Count(c => c.DelayId == d.Id),
                    context.DelayCycles.AsNoTracking().Any(c => c.DelayId == d.Id && c.ClosedAt == null),
                    context.DelayCycles.AsNoTracking()
                        .Where(c => c.DelayId == d.Id && c.ClosedAt == null)
                        .Select(c => c.ExpectedReadyAt)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken);
            if (delays.Count == 0)
            {
                return ActiveDelaySet.Empty;
            }

            var delayIds = delays.Select(d => d.DelayId).ToList();
            var impacts = await (
                from i in context.DelayImpacts.AsNoTracking()
                join crew in context.Crew.AsNoTracking() on i.BlockedCrewId equals crew.Id
                where delayIds.Contains(i.DelayId)
                orderby crew.Name
                select new ActiveDelayImpactRecord(
                    i.Id,
                    i.DelayId,
                    i.BlockedCrewId,
                    crew.Name,
                    i.BlockedWorkItemId,
                    context.DelayImpactVerifications.AsNoTracking().Any(v =>
                        v.DelayImpactId == i.Id
                        && context.DelayCycles.AsNoTracking().Any(c => c.DelayCycleId == v.DelayCycleId && c.ClosedAt == null))))
                .ToListAsync(cancellationToken);

            return new ActiveDelaySet(delays, impacts);
        }

        public async Task<DelayReportSet> GetDelayReportAsync(
            IReadOnlyCollection<int> projectIds, DateTimeOffset? createdFrom, DateTimeOffset? createdBefore, CancellationToken cancellationToken)
        {
            // One scope for both queries, so the impacts query filters in SQL instead of
            // sending every delay ID back as a parameter (SQL Server allows 2,100 per command).
            var scope = context.Delays.AsNoTracking()
                .Where(d => projectIds.Contains(d.ProjectId)
                            && (createdFrom == null || d.CreatedAt >= createdFrom)
                            && (createdBefore == null || d.CreatedAt < createdBefore));

            var delays = await (
                from d in scope
                join p in context.Projects.AsNoTracking() on d.ProjectId equals p.Id
                join status in context.DelayStatus.AsNoTracking() on d.DelayStatusId equals status.Id
                join tag in context.DelayTags.AsNoTracking() on d.DelayTagId equals tag.Id
                join owner in context.Crew.AsNoTracking() on d.OwnerCrewId equals owner.Id
                join segment in context.Segments.AsNoTracking() on d.SegmentId equals segment.Id
                join reporter in context.Users.AsNoTracking() on d.ReportedByUserId equals reporter.Id
                orderby d.CreatedAt descending, d.Id descending
                select new DelayReportRecord(
                    d.Id,
                    d.Number,
                    p.Name,
                    d.Title,
                    status.Name,
                    tag.Name,
                    d.ReportType,
                    owner.Name,
                    segment.Name,
                    d.LocationName,
                    reporter.DisplayName,
                    d.CreatedAt,
                    context.DelayCycles.AsNoTracking()
                        .Where(c => c.DelayId == d.Id)
                        .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.DelayCycleId)
                        .Select(c => c.ClosedAt)
                        .FirstOrDefault(),
                    context.DelayCycles.AsNoTracking().Count(c => c.DelayId == d.Id),
                    context.DelayPhotos.AsNoTracking().Count(ph => ph.DelayId == d.Id)))
                .ToListAsync(cancellationToken);
            if (delays.Count == 0)
            {
                return DelayReportSet.Empty;
            }

            var impacts = await (
                from d in scope
                join i in context.DelayImpacts.AsNoTracking() on d.Id equals i.DelayId
                join crew in context.Crew.AsNoTracking() on i.BlockedCrewId equals crew.Id
                select new { i.DelayId, crew.Name })
                .Distinct()
                .OrderBy(i => i.Name)
                .Select(i => new DelayReportImpactRecord(i.DelayId, i.Name))
                .ToListAsync(cancellationToken);

            return new DelayReportSet(delays, impacts);
        }

        public async Task<IReadOnlyList<string>> GetActiveDelayTagNamesAsync(CancellationToken cancellationToken) =>
            await context.DelayTags.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Id)
                .Select(t => t.Name)
                .ToListAsync(cancellationToken);

        public async Task<TrackingSet> GetProjectTrackingDetailsAsync(int projectId, CancellationToken cancellationToken)
        {
            var delays = await (
                from d in context.Delays.AsNoTracking()
                join status in context.DelayStatus.AsNoTracking() on d.DelayStatusId equals status.Id
                join tag in context.DelayTags.AsNoTracking() on d.DelayTagId equals tag.Id
                join owner in context.Crew.AsNoTracking() on d.OwnerCrewId equals owner.Id
                join segment in context.Segments.AsNoTracking() on d.SegmentId equals segment.Id
                join reporter in context.Users.AsNoTracking() on d.ReportedByUserId equals reporter.Id
                join reporterCrew in context.Crew.AsNoTracking() on d.ReportedByCrewId equals reporterCrew.Id
                where d.ProjectId == projectId
                orderby d.CreatedAt descending, d.Id descending
                select new TrackingDelayRecord(new ActiveDelayRecord(
                    d.Id, d.Number, d.Title, d.Description, status.Code, tag.Code, tag.Name, d.OwnerCrewId, owner.Name,
                    d.ReportType, d.LocationName ?? segment.Name, segment.Name, d.SiteMapId, d.PinX, d.PinY, d.CreatedAt,
                    context.DelayCycles.AsNoTracking().Count(c => c.DelayId == d.Id),
                    context.DelayCycles.AsNoTracking().Any(c => c.DelayId == d.Id && c.ClosedAt == null),
                    context.DelayCycles.AsNoTracking().Where(c => c.DelayId == d.Id).OrderByDescending(c => c.DelayCycleId)
                        .Select(c => c.ExpectedReadyAt).FirstOrDefault()),
                    d.ReportedByUserId, reporter.DisplayName, reporterCrew.Name)).ToListAsync(cancellationToken);
            if (delays.Count == 0)
            {
                return TrackingSet.Empty;
            }

            // Sequential bulk projections on the scoped context; never one query per delay.
            var impacts = await (
                from i in context.DelayImpacts.AsNoTracking()
                join crew in context.Crew.AsNoTracking() on i.BlockedCrewId equals crew.Id
                where i.ProjectId == projectId && context.Delays.AsNoTracking().Any(d => d.Id == i.DelayId && d.ProjectId == projectId)
                orderby crew.Name, i.Id
                select new TrackingImpactRecord(i.Id, i.DelayId, crew.Name, i.BlockedWorkItemId,
                    context.WorkItems.AsNoTracking().Where(w => w.Id == i.BlockedWorkItemId).Select(w => w.Title).FirstOrDefault()))
                .ToListAsync(cancellationToken);
            var cycles = await (
                from c in context.DelayCycles.AsNoTracking()
                join d in context.Delays.AsNoTracking() on c.DelayId equals d.Id
                where d.ProjectId == projectId
                orderby c.DelayCycleId
                select new TrackingCycleRecord(c.DelayCycleId, c.DelayId, c.CreatedAt, c.OpenReason,
                    c.AcknowledgmentChoice, c.AcknowledgmentNote, c.AcknowledgedAt,
                    context.Users.AsNoTracking().Where(u => u.Id == c.AcknowledgedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    c.ExpectedReadyAt, c.ClosedAt)).ToListAsync(cancellationToken);
            var verifications = await (
                from v in context.DelayImpactVerifications.AsNoTracking()
                join d in context.Delays.AsNoTracking() on v.DelayId equals d.Id
                join user in context.Users.AsNoTracking() on v.VerifiedByUserId equals user.Id
                where d.ProjectId == projectId
                orderby v.VerifiedAt
                select new TrackingVerificationRecord(v.DelayImpactId, v.DelayCycleId, user.DisplayName, v.VerifiedAt, v.VerificationNote))
                .ToListAsync(cancellationToken);
            var photos = await (
                from p in context.DelayPhotos.AsNoTracking()
                join d in context.Delays.AsNoTracking() on p.DelayId equals d.Id
                where d.ProjectId == projectId
                orderby p.CreatedAt, p.Id
                select new TrackingPhotoRecord(p.Id, p.DelayId, p.Purpose, p.OriginalUrl, p.ThumbnailUrl, p.FileName,
                    p.CreatedAt, p.Latitude, p.Longitude)).ToListAsync(cancellationToken);
            var events = await (
                from e in context.ActivityEvents.AsNoTracking()
                join d in context.Delays.AsNoTracking() on e.DelayId equals d.Id
                join actor in context.Users.AsNoTracking() on e.ActorUserId equals actor.Id
                where e.ProjectId == projectId && d.ProjectId == projectId
                orderby e.CreatedAt, e.Id
                select new TrackingEventRecord(e.Id, e.DelayId, e.DelayCycleId, e.EventType, actor.DisplayName,
                    e.CreatedAt, e.OldExpectedReadyAt, e.NewExpectedReadyAt)).ToListAsync(cancellationToken);
            var maps = await context.SiteMaps.AsNoTracking().Where(m => m.ProjectId == projectId)
                .Select(m => new TrackingSiteMapRecord(m.Id, m.Name, m.Uri)).ToListAsync(cancellationToken);
            return new TrackingSet(delays, impacts, cycles, verifications, photos, events, maps);
        }
    }
}
