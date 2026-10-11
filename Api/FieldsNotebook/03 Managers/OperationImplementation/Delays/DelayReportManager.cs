using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Projects;
using Common.Roles;
using DataContract.Delays;
using DataContract.Home;

namespace Managers.Delays;

public sealed class DelayReportManager(AccessorFactory accessors) : IDelayReportManager
{
    public async Task<DelayReportOptionsResponse> GetReportOptionsAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await RequireReporterAsync(userId, projectId, cancellationToken);
        var data = await accessors.CreateAccessor<IDelayAccessor>().GetReportOptionsAsync(projectId, cancellationToken);
        return new(projectId, access.ProjectName,
            data.Crews.Select(c => new ReportCrewResponse(c.Id, c.Name)).ToList(),
            data.WorkItems.Select(w => new ReportWorkItemResponse(w.Id, w.Title, w.CrewIds)).ToList(),
            data.Segments.Select(s => new ReportSegmentResponse(s.Id, s.Name)).ToList(),
            data.Tags.Select(t => new ReportTagResponse(t.Id, t.Code, t.Name)).ToList(),
            data.SiteMap is { } map ? new(map.Id, map.Name, map.Uri) : null,
            data.People.Select(p => new ReportPersonResponse(p.UserId, p.DisplayName,
                p.CrewId, p.CrewName, p.RoleName)).ToList());
    }

    public async Task<DelaySummaryResponse> CreateReportAsync(int userId, int projectId, DelayReportRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireReporterAsync(userId, projectId, cancellationToken);
        var accessor = accessors.CreateAccessor<IDelayAccessor>();
        var data = await accessor.GetReportOptionsAsync(projectId, cancellationToken);
        ValidateRequest(request, data);
        var reporterCrew = RequireCrewAuthority(userId, request, data);
        var createdAt = DateTimeOffset.UtcNow;
        var report = ToRecord(projectId, userId, reporterCrew.Id, request, data, createdAt);
        var saved = await accessor.CreateReportAsync(report, cancellationToken);
        // No external effects or fallible database reads after a successful commit.
        return ToSummary(saved, access, request, data, createdAt);
    }

    private async Task<ProjectAccessRecord> RequireReporterAsync(int userId, int projectId, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);
        var access = await accessors.CreateAccessor<IProjectAccessAccessor>().GetProjectAccessAsync(userId, projectId, token)
            ?? throw new ProjectAccessDeniedException(projectId);
        if (access.RoleName != RoleNames.Foreman) throw new DelayReportForbiddenException("Only a Foreman reports a delay.");
        return access;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new DelayReportValidationException(message);
    }

    private static void ValidateRequest(DelayReportRequest r, DelayReportOptionsRecord data)
    {
        Require(!string.IsNullOrWhiteSpace(r.Title) && r.Title.Length <= 200, "Enter a delay title of up to 200 characters.");
        Require(!string.IsNullOrWhiteSpace(r.Description) && r.Description.Length <= 4000, "Describe the blocker in up to 4000 characters.");
        Require(data.Tags.Any(t => t.Id == r.DelayTagId), "Choose an active reason.");
        Require(data.Crews.Any(c => c.Id == r.OwnerCrewId), "Choose a responsible crew in this project.");
        Require(data.Segments.Any(s => s.Id == r.SegmentId), "Choose a segment in this project.");
        ValidateImpacts(r, data);
        ValidateReportType(r);
        ValidateLocation(r, data);
        DelayPhotoRules.Validate(r.Photos, "A delay report needs one to five photos.");
        Require(r.NotifyUserIds.All(id => data.People.Any(p => p.UserId == id)), "Notification recipients must belong to this project.");
    }

    private static void ValidateImpacts(DelayReportRequest r, DelayReportOptionsRecord data)
    {
        Require(r.Impacts.Count > 0, "Choose at least one affected crew.");
        Require(r.Impacts.DistinctBy(i => (i.BlockedCrewId, i.BlockedWorkItemId)).Count() == r.Impacts.Count,
            "Each affected crew and work item must be listed once.");
        foreach (var impact in r.Impacts)
        {
            Require(impact.BlockedCrewId != r.OwnerCrewId, "The responsible crew cannot block itself.");
            Require(data.Crews.Any(c => c.Id == impact.BlockedCrewId), "Affected crews must belong to this project.");
            ValidateWorkItem(impact.BlockedWorkItemId, impact.BlockedCrewId, data);
        }
        ValidateWorkItem(r.BlockingWorkItemId, r.OwnerCrewId, data);
    }

    private static void ValidateWorkItem(int? workItemId, int crewId, DelayReportOptionsRecord data) =>
        Require(workItemId is null || data.WorkItems.Any(w => w.Id == workItemId && w.CrewIds.Contains(crewId)),
            "Choose an unfinished work item assigned to that crew in this project.");

    private static void ValidateReportType(DelayReportRequest r)
    {
        Require(r.ReportType is DelayReportTypes.AfterArrival or DelayReportTypes.AdvanceNotice, "Choose a valid report type.");
        Require(r.ExpectedReadyAt is null,
            "The responsible Superintendent sets the tentative completion time when acknowledging the delay.");
    }

    private static void ValidateLocation(DelayReportRequest r, DelayReportOptionsRecord data)
    {
        Require(r.LocationName is null || r.LocationName.Length <= 200, "Location names allow up to 200 characters.");
        ValidatePin(r, data);
        Require(r.PinX is not null || !string.IsNullOrWhiteSpace(r.LocationName) || r.Photos.Any(p => p.Latitude is not null),
            "Place a site pin or enter a location name when photos have no GPS.");
    }

    private static void ValidatePin(DelayReportRequest r, DelayReportOptionsRecord data)
    {
        Require(r.PinX.HasValue == r.PinY.HasValue, "Both pin coordinates are required.");
        Require(r.PinX is null or (>= 0 and <= 1), "Pin X must be between zero and one.");
        Require(r.PinY is null or (>= 0 and <= 1), "Pin Y must be between zero and one.");
        Require(r.SiteMapId is null || r.SiteMapId == data.SiteMap?.Id, "Choose the active map in this project.");
        // The bundled site image has no database map record; its pin still persists.
    }

    private static ReportCrewRecord RequireCrewAuthority(int userId, DelayReportRequest r, DelayReportOptionsRecord data)
    {
        var crew = data.Crews.SingleOrDefault(c => c.ForemanId == userId)
            ?? throw new DelayReportForbiddenException("You must lead an active crew in this project.");
        if (crew.Id != r.OwnerCrewId && !r.Impacts.Any(i => i.BlockedCrewId == crew.Id))
            throw new DelayReportForbiddenException("Name your own crew as responsible or affected.");
        return crew;
    }

    private static string LocationSource(DelayReportRequest r)
    {
        if (r.PinX is not null) return "ManualPin";
        return string.IsNullOrWhiteSpace(r.LocationName) ? "PhotoGps" : "ManualEntry";
    }

    private static IReadOnlyList<int> Recipients(DelayReportRequest r, DelayReportOptionsRecord data)
    {
        var owner = data.Crews.Single(c => c.Id == r.OwnerCrewId);
        var result = r.NotifyUserIds.Append(owner.SuperintendentId);
        if (r.ReportType == DelayReportTypes.AdvanceNotice)
            result = result.Concat(data.Crews.Where(c => r.Impacts.Any(i => i.BlockedCrewId == c.Id)).Select(c => c.SuperintendentId));
        return result.Distinct().ToList();
    }

    private static DelayReportRecord ToRecord(int projectId, int userId, int reporterCrewId, DelayReportRequest r,
        DelayReportOptionsRecord data, DateTimeOffset createdAt) =>
        new(projectId, userId, reporterCrewId, r.Title.Trim(), r.Description.Trim(), r.OwnerCrewId,
            r.DelayTagId, r.ReportType, r.BlockingWorkItemId, r.SegmentId, r.SiteMapId, r.PinX, r.PinY,
            LocationSource(r), r.LocationName?.Trim(), createdAt,
            r.Impacts.Select(i => new ReportImpactRecord(i.BlockedCrewId, i.BlockedWorkItemId)).ToList(),
            DelayPhotoRules.ToUploads(r.Photos), Recipients(r, data));

    private static DelaySummaryResponse ToSummary(ReportSavedRecord saved, ProjectAccessRecord access,
        DelayReportRequest r, DelayReportOptionsRecord data, DateTimeOffset createdAt)
    {
        var crewNames = data.Crews.ToDictionary(c => c.Id, c => c.Name);
        var tag = data.Tags.Single(t => t.Id == r.DelayTagId);
        var segment = data.Segments.Single(s => s.Id == r.SegmentId);
        return new(saved.Id, saved.Number, r.Title.Trim(), DelayStatusCodes.Raised, tag.Code, tag.Name,
            crewNames[r.OwnerCrewId], r.Impacts.Select(i => crewNames[i.BlockedCrewId]).Distinct().ToList(),
            r.Impacts.Select((i, index) => new DelayImpactResponse(saved.ImpactIds[index], crewNames[i.BlockedCrewId], i.BlockedWorkItemId)).ToList(),
            string.IsNullOrWhiteSpace(r.LocationName) ? segment.Name : r.LocationName.Trim(), access.ProjectId,
            access.ProjectName, false, r.ReportType, createdAt, null,
            r.PinX is { } x && r.PinY is { } y ? new DelayPinResponse(x, y) : null, r.SiteMapId, segment.Name);
    }
}
