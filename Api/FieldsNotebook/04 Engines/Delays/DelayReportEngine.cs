using Common.Delays;
using Common.Errors;
using Common.Results;
using Common.Roles;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Mapping.ClientToDomainMapping;

namespace Engines.Delays;

/// <summary>
/// Who may raise a delay and what a new delay must name, checked against the project's live options.
/// Field lengths, ranges and pairs are checked on the request contract before any of this runs.
/// </summary>
public static class DelayReportEngine
{
    /// <summary>Only a Foreman reports a delay, so only a Foreman reads the report form or transcribes one.</summary>
    /// <returns>The refusal, or null when the user may report.</returns>
    public static RequestError? CheckReporter(ProjectAccessRecord access)
    {
        return access.RoleName == RoleNames.Foreman ? null : RequestError.Forbidden("Only a Foreman reports a delay.");
    }

    /// <summary>Checks the report against the project's options and returns the write that raises it.</summary>
    public static Result<RaiseDelayWrite> Raise(int actorUserId, int projectId, RaiseDelayRequest request,
        DelayReportOptionsRecord options, DateTimeOffset createdAt)
    {
        if (ValidateChoices(request, options) is { } invalid)
        {
            return invalid;
        }

        var reporterCrew = FindReporterCrew(actorUserId, request, options);
        if (reporterCrew.IsFailure)
        {
            return reporterCrew.Error;
        }

        return new RaiseDelayWrite(projectId, actorUserId, reporterCrew.Data.Id, request.Title.Trim(), request.Description.Trim(),
            request.OwnerCrewId, request.DelayTagId, request.ReportType, request.BlockingWorkItemId, request.SegmentId,
            request.SiteMapId, request.PinX, request.PinY, LocationSource(request), request.LocationName?.Trim(), createdAt,
            request.Impacts.ToDomain(), request.Photos.ToDomain(), Recipients(request, options));
    }

    #region Private Methods

    private static RequestError? ValidateChoices(RaiseDelayRequest r, DelayReportOptionsRecord options)
    {
        if (options.Tags.All(t => t.Id != r.DelayTagId))
        {
            return RequestError.InvalidRequest("Choose an active reason.");
        }

        if (options.Crews.All(c => c.Id != r.OwnerCrewId))
        {
            return RequestError.InvalidRequest("Choose a responsible crew in this project.");
        }

        if (options.Segments.All(s => s.Id != r.SegmentId))
        {
            return RequestError.InvalidRequest("Choose a segment in this project.");
        }

        if (ValidateImpacts(r, options) is { } invalidImpact)
        {
            return invalidImpact;
        }

        // The bundled site image has no database map record; its pin still persists.
        if (r.SiteMapId is not null && r.SiteMapId != options.SiteMap?.Id)
        {
            return RequestError.InvalidRequest("Choose the active map in this project.");
        }

        if (DelayPhotoRules.Validate(r.Photos, "A delay report needs one to five photos.") is { } invalidPhoto)
        {
            return invalidPhoto;
        }

        if (r.NotifyUserIds.Any(id => options.People.All(p => p.UserId != id)))
        {
            return RequestError.InvalidRequest("Notification recipients must belong to this project.");
        }

        return null;
    }

    private static RequestError? ValidateImpacts(RaiseDelayRequest r, DelayReportOptionsRecord options)
    {
        if (r.Impacts.Count == 0)
        {
            return RequestError.InvalidRequest("Choose at least one affected crew.");
        }

        if (r.Impacts.DistinctBy(i => (i.BlockedCrewId, i.BlockedWorkItemId)).Count() != r.Impacts.Count)
        {
            return RequestError.InvalidRequest("Each affected crew and work item must be listed once.");
        }

        foreach (var impact in r.Impacts)
        {
            if (impact.BlockedCrewId == r.OwnerCrewId)
            {
                return RequestError.InvalidRequest("The responsible crew cannot block itself.");
            }

            if (options.Crews.All(c => c.Id != impact.BlockedCrewId))
            {
                return RequestError.InvalidRequest("Affected crews must belong to this project.");
            }

            if (ValidateWorkItem(impact.BlockedWorkItemId, impact.BlockedCrewId, options) is { } invalidWorkItem)
            {
                return invalidWorkItem;
            }
        }

        return ValidateWorkItem(r.BlockingWorkItemId, r.OwnerCrewId, options);
    }

    private static RequestError? ValidateWorkItem(int? workItemId, int crewId, DelayReportOptionsRecord options)
    {
        if (workItemId is null || options.WorkItems.Any(w => w.Id == workItemId && w.CrewIds.Contains(crewId)))
        {
            return null;
        }

        return RequestError.InvalidRequest("Choose an unfinished work item assigned to that crew in this project.");
    }

    private static Result<ReportCrewRecord> FindReporterCrew(int userId, RaiseDelayRequest r, DelayReportOptionsRecord options)
    {
        var crew = options.Crews.SingleOrDefault(c => c.ForemanId == userId);
        if (crew is null)
        {
            return RequestError.Forbidden("You must lead an active crew in this project.");
        }

        if (crew.Id != r.OwnerCrewId && !r.Impacts.Any(i => i.BlockedCrewId == crew.Id))
        {
            return RequestError.Forbidden("Name your own crew as responsible or affected.");
        }

        return crew;
    }

    private static string LocationSource(RaiseDelayRequest r)
    {
        if (r.PinX is not null) return "ManualPin";
        return string.IsNullOrWhiteSpace(r.LocationName) ? "PhotoGps" : "ManualEntry";
    }

    /// <summary>The responsible Superintendent always hears; an advance notice also tells the affected crews' Superintendents.</summary>
    private static IReadOnlyList<int> Recipients(RaiseDelayRequest r, DelayReportOptionsRecord options)
    {
        var owner = options.Crews.Single(c => c.Id == r.OwnerCrewId);
        var result = r.NotifyUserIds.Append(owner.SuperintendentId);
        if (DelayRulesEngine.IsAdvanceNotice(r.ReportType))
        {
            result = result.Concat(options.Crews.Where(c => r.Impacts.Any(i => i.BlockedCrewId == c.Id)).Select(c => c.SuperintendentId));
        }

        return result.Distinct().ToList();
    }

    #endregion
}
