using Common.Delays;
using Common.Roles;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utilities.OperationContract;

namespace Accessors.Delays;

public partial class DelayAccessor
{
    public async Task<DelayReportOptionsRecord> GetReportOptionsAsync(int projectId, CancellationToken cancellationToken)
    {
        var crews = await context.Crew.AsNoTracking().Where(c => c.ProjectId == projectId && c.IsActive)
            .OrderBy(c => c.Name).Select(c => new ReportCrewRecord(c.Id, c.Name, c.ForemanLeadId, c.SuperintendentUserId))
            .ToListAsync(cancellationToken);
        var segments = await context.Segments.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActive)
            .OrderBy(s => s.Name).Select(s => new ReportSegmentRecord(s.Id, s.Name)).ToListAsync(cancellationToken);
        var tags = await context.DelayTags.AsNoTracking().Where(t => t.IsActive)
            .OrderBy(t => t.Id).Select(t => new ReportTagRecord(t.Id, t.Code, t.Name)).ToListAsync(cancellationToken);
        var map = await context.SiteMaps.AsNoTracking().Where(m => m.ProjectId == projectId && m.IsActive)
            .OrderBy(m => m.Id).Select(m => new ReportMapRecord(m.Id, m.Name, m.Uri)).FirstOrDefaultAsync(cancellationToken);
        var workItems = await ReadReportWorkItemsAsync(projectId, cancellationToken);
        var people = await ReadReportPeopleAsync(projectId, cancellationToken);
        return new(crews, workItems, segments, tags, map, people);
    }

    private async Task<IReadOnlyList<ReportWorkItemRecord>> ReadReportWorkItemsAsync(int projectId, CancellationToken token)
    {
        var items = await context.WorkItems.AsNoTracking().Where(w => w.ProjectId == projectId && !w.IsCompleted)
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

    /// <summary>Owns the single scoped transaction for every database row in the initial report.</summary>
    public async Task<ReportSavedRecord> CreateReportAsync(DelayReportRecord report, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var photoStorage = new DelayPhotoStorage(Utilities.CreateManager<IAzureBlobStorageUtility>(), logger);
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

    private async Task<ReportSavedRecord> SaveReportRowsAsync(DelayReportRecord report, DelayPhotoStorage photoStorage,
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

    private static Delay NewDelay(DelayReportRecord r, int statusId) => new()
    {
        Number = $"FN-{Guid.NewGuid():N}"[..27], ProjectId = r.ProjectId, Title = r.Title,
        Description = r.Description, ReportedByUserId = r.ActorUserId, ReportedByCrewId = r.ReporterCrewId,
        OwnerCrewId = r.OwnerCrewId, DelayTagId = r.DelayTagId, ReportType = r.ReportType,
        BlockingWorkItemId = r.BlockingWorkItemId, DelayStatusId = statusId, SegmentId = r.SegmentId,
        SiteMapId = r.SiteMapId, PinX = r.PinX, PinY = r.PinY, LocationSource = r.LocationSource,
        LocationName = r.LocationName, CreatedAt = r.CreatedAt,
    };

    private static ActivityEvent NewReportEvent(DelayReportRecord r, Delay delay, DelayCycle cycle) => new()
    {
        ProjectId = r.ProjectId, DelayId = delay.Id, DelayCycleId = cycle.DelayCycleId,
        ActorUserId = r.ActorUserId, ToDelayStatusId = delay.DelayStatusId, CreatedAt = r.CreatedAt,
        EventType = r.ReportType == DelayReportTypes.AdvanceNotice ? "delay.advanceNoticeRaised" : "delay.raised",
    };

    private static DelayPhoto NewPhoto(DelayReportRecord r, ReportPhotoRecord p, int delayId, int cycleId) => new()
    {
        DelayId = delayId, DelayCycleId = cycleId, Purpose = "InitialReport", CreatedByUserId = r.ActorUserId,
        OriginalUrl = p.OriginalUrl, ThumbnailUrl = p.ThumbnailUrl, FileName = p.FileName,
        Latitude = p.Latitude, Longitude = p.Longitude, CreatedAt = r.CreatedAt,
    };
}
