using Common.Delays;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Reports;
using Database;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utilities;
using Utilities.OperationContract;

namespace Accessors.Delays
{
    public class DelayAccessor(FieldNotebookContext context, UtilityFactory utilityFactory,
        ILogger<DelayAccessor> logger) : IDelayAccessor
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

        public async Task<DelayReportSet> GetDelayReportAsync(DelayReportQueryRecord query, CancellationToken cancellationToken)
        {
            // One scope for both queries, so the impacts query filters in SQL instead of
            // sending every delay ID back as a parameter (SQL Server allows 2,100 per command).
            var scope = context.Delays.AsNoTracking()
                .Where(d => query.ProjectIds.Contains(d.ProjectId)
                            && (query.CreatedFrom == null || d.CreatedAt >= query.CreatedFrom)
                            && (query.CreatedBefore == null || d.CreatedAt < query.CreatedBefore));

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

        public Task<TrackingSet> GetProjectTrackingDetailsAsync(int projectId, CancellationToken cancellationToken) =>
            ReadTrackingAsync(projectId, null, cancellationToken);

        public Task<TrackingSet> GetDelayTrackingDetailsAsync(int projectId, int delayId, CancellationToken cancellationToken) =>
            ReadTrackingAsync(projectId, delayId, cancellationToken);

        public async Task<DelayReportOptionsRecord> GetReportOptionsAsync(int projectId, CancellationToken cancellationToken)
        {
            var crews = await ReadReportCrewsAsync(projectId, cancellationToken);
            var segments = await context.Segments.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActive)
                .OrderBy(s => s.Name).Select(s => new ReportSegmentRecord(s.Id, s.Name)).ToListAsync(cancellationToken);
            var tags = await ReadReportTagsAsync(cancellationToken);
            var map = await context.SiteMaps.AsNoTracking().Where(m => m.ProjectId == projectId && m.IsActive)
                .OrderBy(m => m.Id).Select(m => new ReportMapRecord(m.Id, m.Name, m.Uri)).FirstOrDefaultAsync(cancellationToken);
            var workItems = await ReadReportWorkItemsAsync(projectId, cancellationToken);
            var people = await ReadReportPeopleAsync(projectId, cancellationToken);
            return new(crews, workItems, segments, tags, map, people);
        }

        public async Task<ReportChoicesRecord> GetReportChoicesAsync(int projectId, CancellationToken cancellationToken) =>
            new(await ReadReportCrewsAsync(projectId, cancellationToken), await ReadReportTagsAsync(cancellationToken));

        /// <summary>Owns the single scoped transaction for every database row in the initial report.</summary>
        public async Task<ReportSavedRecord> CreateDelayAsync(RaiseDelayWrite report, CancellationToken cancellationToken)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var photoStorage = new DelayPhotoStorage(utilityFactory.CreateUtility<IAzureBlobStorageUtility>(), logger);
            try
            {
                var saved = await SaveReportRowsAsync(report, photoStorage, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return saved;
            }
            catch
            {
                await photoStorage.CleanUpAsync();
                throw;
            }
        }

        #region Private Methods

        // A null delay ID reads every delay in the project.
        private async Task<TrackingSet> ReadTrackingAsync(int projectId, int? delayId, CancellationToken cancellationToken)
        {
            var delays = await (
                from d in context.Delays.AsNoTracking()
                join status in context.DelayStatus.AsNoTracking() on d.DelayStatusId equals status.Id
                join tag in context.DelayTags.AsNoTracking() on d.DelayTagId equals tag.Id
                join owner in context.Crew.AsNoTracking() on d.OwnerCrewId equals owner.Id
                join segment in context.Segments.AsNoTracking() on d.SegmentId equals segment.Id
                join reporter in context.Users.AsNoTracking() on d.ReportedByUserId equals reporter.Id
                join reporterCrew in context.Crew.AsNoTracking() on d.ReportedByCrewId equals reporterCrew.Id
                where d.ProjectId == projectId && (delayId == null || d.Id == delayId)
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
                    && (delayId == null || i.DelayId == delayId)
                orderby crew.Name, i.Id
                select new TrackingImpactRecord(i.Id, i.DelayId, crew.Name, i.BlockedWorkItemId,
                    context.WorkItems.AsNoTracking().Where(w => w.Id == i.BlockedWorkItemId).Select(w => w.Title).FirstOrDefault()))
                .ToListAsync(cancellationToken);
            var cycles = await (
                from c in context.DelayCycles.AsNoTracking()
                join d in context.Delays.AsNoTracking() on c.DelayId equals d.Id
                where d.ProjectId == projectId && (delayId == null || d.Id == delayId)
                orderby c.DelayCycleId
                select new TrackingCycleRecord(c.DelayCycleId, c.DelayId, c.CreatedAt, c.OpenReason,
                    c.AcknowledgmentChoice, c.AcknowledgmentNote, c.AcknowledgedAt,
                    context.Users.AsNoTracking().Where(u => u.Id == c.AcknowledgedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    c.ExpectedReadyAt, c.ClosedAt)).ToListAsync(cancellationToken);
            var verifications = await (
                from v in context.DelayImpactVerifications.AsNoTracking()
                join d in context.Delays.AsNoTracking() on v.DelayId equals d.Id
                join user in context.Users.AsNoTracking() on v.VerifiedByUserId equals user.Id
                where d.ProjectId == projectId && (delayId == null || d.Id == delayId)
                orderby v.VerifiedAt
                select new TrackingVerificationRecord(v.DelayImpactId, v.DelayCycleId, user.DisplayName, v.VerifiedAt, v.VerificationNote))
                .ToListAsync(cancellationToken);
            var photos = await (
                from p in context.DelayPhotos.AsNoTracking()
                join d in context.Delays.AsNoTracking() on p.DelayId equals d.Id
                where d.ProjectId == projectId && (delayId == null || d.Id == delayId)
                orderby p.CreatedAt, p.Id
                select new TrackingPhotoRecord(p.Id, p.DelayId, p.Purpose, p.OriginalUrl, p.ThumbnailUrl, p.FileName,
                    p.CreatedAt, p.Latitude, p.Longitude)).ToListAsync(cancellationToken);
            var events = await (
                from e in context.ActivityEvents.AsNoTracking()
                join d in context.Delays.AsNoTracking() on e.DelayId equals d.Id
                join actor in context.Users.AsNoTracking() on e.ActorUserId equals actor.Id
                where e.ProjectId == projectId && d.ProjectId == projectId && (delayId == null || d.Id == delayId)
                orderby e.CreatedAt, e.Id
                select new TrackingEventRecord(e.Id, e.DelayId, e.DelayCycleId, e.EventType, actor.DisplayName,
                    e.CreatedAt, e.OldExpectedReadyAt, e.NewExpectedReadyAt)).ToListAsync(cancellationToken);
            var maps = await context.SiteMaps.AsNoTracking().Where(m => m.ProjectId == projectId)
                .Select(m => new TrackingSiteMapRecord(m.Id, m.Name, m.Uri)).ToListAsync(cancellationToken);
            return new TrackingSet(delays, impacts, cycles, verifications, photos, events, maps);
        }

        private async Task<IReadOnlyList<ReportCrewRecord>> ReadReportCrewsAsync(int projectId, CancellationToken token) =>
            await context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive)
                .OrderBy(c => c.Name).Select(c => new ReportCrewRecord(c.Id, c.Name, c.ForemanLeadId, c.SuperintendentUserId))
                .ToListAsync(token);

        private async Task<IReadOnlyList<ReportTagRecord>> ReadReportTagsAsync(CancellationToken token) =>
            await context.DelayTags.AsNoTracking().Where(t => t.IsActive)
                .OrderBy(t => t.Id).Select(t => new ReportTagRecord(t.Id, t.Code, t.Name)).ToListAsync(token);

        private async Task<IReadOnlyList<ReportWorkItemRecord>> ReadReportWorkItemsAsync(int projectId, CancellationToken token)
        {
            var items = await context.WorkItems.AsNoTracking().Where(w => w.ProjectId == projectId && w.IsCompleted == false)
                .OrderBy(w => w.Title).Select(w => new { w.Id, w.Title }).ToListAsync(token);
            var assignments = await context.WorkItemTeamAssignment.AsNoTracking().Where(a => a.ProjectId == projectId)
                .Select(a => new { a.WorkItemId, a.CrewId }).ToListAsync(token);
            var crews = assignments.ToLookup(a => a.WorkItemId, a => a.CrewId);
            return items.Select(w => new ReportWorkItemRecord(w.Id, w.Title, crews[w.Id].ToList())).ToList();
        }

        private async Task<IReadOnlyList<ReportPersonRecord>> ReadReportPeopleAsync(int projectId, CancellationToken token)
        {
            var people = await (from up in context.UserProjects.AsNoTracking()
                join u in context.Users.AsNoTracking() on up.UserId equals u.Id
                join ur in context.UserRoles.AsNoTracking() on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
                join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where up.ProjectId == projectId && up.IsActive && u.IsActive && ur.IsActive && r.IsActive
                    && (r.Name == RoleNames.Foreman || r.Name == RoleNames.Superintendent)
                select new { u.Id, u.DisplayName, RoleName = r.Name }).ToListAsync(token);
            var crews = await context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive)
                .Select(c => new { c.Id, c.Name, c.ForemanLeadId, c.SuperintendentUserId }).ToListAsync(token);
            return (from p in people from c in crews
                where p.Id == (p.RoleName == RoleNames.Foreman ? c.ForemanLeadId : c.SuperintendentUserId)
                select new ReportPersonRecord(p.Id, p.DisplayName, c.Id, c.Name, p.RoleName)).ToList();
        }

        private async Task<ReportSavedRecord> SaveReportRowsAsync(RaiseDelayWrite report, DelayPhotoStorage photoStorage,
            CancellationToken cancellationToken)
        {
            var statusId = await context.DelayStatus.Where(s => s.Code == DelayStatusCodes.Raised && s.IsActive)
                .Select(s => s.Id).SingleAsync(cancellationToken);
            var delay = NewDelay(report, statusId);
            context.Delays.Add(delay);
            await context.SaveChangesAsync(cancellationToken);
            // The database-generated identity supplies the submitted report number.
            delay.Number = $"FN-{delay.Id:000000}";
            var cycle = new DelayCycle { DelayId = delay.Id, CreatedAt = report.CreatedAt,
                CreatedBy = report.ActorUserId };
            var impacts = report.Impacts.Select(i => new DelayImpact { DelayId = delay.Id,
                ProjectId = report.ProjectId, BlockedCrewId = i.CrewId, BlockedWorkItemId = i.WorkItemId }).ToList();
            context.DelayCycles.Add(cycle);
            context.DelayImpacts.AddRange(impacts);
            await context.SaveChangesAsync(cancellationToken);
            // The delay and cycle now have database identities. Upload before committing
            // so failures roll back the whole report instead of leaving missing photos.
            var workItem = report.BlockingWorkItemId?.ToString() ?? "unassigned";
            var prefix = $"project-{report.ProjectId}/{report.CreatedAt:yyyy-MM-dd}/work-item-{workItem}/report-{delay.Id}/images";
            var photos = await photoStorage.UploadAsync(report.Photos, prefix, cancellationToken);
            var activity = NewReportEvent(report, delay, cycle);
            context.ActivityEvents.Add(activity);
            context.DelayPhotos.AddRange(photos.Select(p => NewPhoto(report, p, delay.Id, cycle.DelayCycleId)));
            await context.SaveChangesAsync(cancellationToken);
            context.Notifications.AddRange(report.RecipientIds.Select(id => new Notification { UserId = id,
                ProjectId = report.ProjectId, DelayId = delay.Id, ActivityEventId = activity.Id, CreatedAt = report.CreatedAt }));
            await context.SaveChangesAsync(cancellationToken);
            return new(delay.Id, delay.Number, impacts.Select(i => i.Id).ToList());
        }

        private static Delay NewDelay(RaiseDelayWrite r, int statusId) => new()
        {
            Number = $"FN-{Guid.NewGuid():N}"[..27], ProjectId = r.ProjectId, Title = r.Title,
            Description = r.Description, ReportedByUserId = r.ActorUserId, ReportedByCrewId = r.ReporterCrewId,
            OwnerCrewId = r.OwnerCrewId, DelayTagId = r.DelayTagId, ReportType = r.ReportType,
            BlockingWorkItemId = r.BlockingWorkItemId, DelayStatusId = statusId, SegmentId = r.SegmentId,
            SiteMapId = r.SiteMapId, PinX = r.PinX, PinY = r.PinY, LocationSource = r.LocationSource,
            LocationName = r.LocationName, CreatedAt = r.CreatedAt,
        };

        private static ActivityEvent NewReportEvent(RaiseDelayWrite r, Delay delay, DelayCycle cycle) => new()
        {
            ProjectId = r.ProjectId, DelayId = delay.Id, DelayCycleId = cycle.DelayCycleId,
            ActorUserId = r.ActorUserId, ToDelayStatusId = delay.DelayStatusId, CreatedAt = r.CreatedAt,
            EventType = r.ReportType == DelayReportTypes.AdvanceNotice ? "delay.advanceNoticeRaised" : "delay.raised",
        };

        private static DelayPhoto NewPhoto(RaiseDelayWrite r, ReportPhotoRecord p, int delayId, int cycleId) => new()
        {
            DelayId = delayId, DelayCycleId = cycleId, Purpose = "InitialReport", CreatedByUserId = r.ActorUserId,
            OriginalUrl = p.OriginalUrl, ThumbnailUrl = p.ThumbnailUrl, FileName = p.FileName,
            Latitude = p.Latitude, Longitude = p.Longitude, CreatedAt = r.CreatedAt,
        };

        #endregion
    }
}
