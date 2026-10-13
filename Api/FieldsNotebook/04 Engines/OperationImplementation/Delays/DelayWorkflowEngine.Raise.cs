using Common.Delays;
using Common.Errors;
using Common.Results;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;

namespace Engines.Delays;

/// <summary>
/// Who may raise a delay and what a new delay must name, checked against the project's live options.
/// Field lengths, ranges and pairs are checked on the request contract before any of this runs.
/// </summary>
public sealed partial class DelayWorkflowEngine
{
    public RequestError? CheckReporter(ProjectAccessRecord access)
    {
        return access.RoleName == RoleNames.Foreman ? null : RequestError.Forbidden("Only a Foreman reports a delay.");
    }

    public Result<RaiseDelayWrite> Raise(int actorUserId, int projectId, RaiseDelayDraft draft, DelayReportOptionsRecord options,
        DateTimeOffset createdAt)
    {
        if (ValidateChoices(draft, options) is { } invalid)
        {
            return invalid;
        }

        var reporterCrew = FindReporterCrew(actorUserId, draft, options);
        if (reporterCrew.IsFailure)
        {
            return reporterCrew.Error;
        }

        return new RaiseDelayWrite(projectId, actorUserId, reporterCrew.Data.Id, draft.Title.Trim(), draft.Description.Trim(),
            draft.OwnerCrewId, draft.DelayTagId, draft.ReportType, draft.BlockingWorkItemId, draft.SegmentId,
            draft.SiteMapId, draft.PinX, draft.PinY, LocationSource(draft), draft.LocationName?.Trim(), createdAt,
            draft.Impacts, draft.Photos, Recipients(draft, options),
            IsAdvanceNotice(draft.ReportType) ? DelayEventTypes.AdvanceNoticeRaised : DelayEventTypes.Raised);
    }

    #region Private Methods

    private static bool IsAdvanceNotice(string reportType) => reportType == DelayReportTypes.AdvanceNotice;

    private static RequestError? ValidateChoices(RaiseDelayDraft draft, DelayReportOptionsRecord options)
    {
        if (options.Tags.All(t => t.Id != draft.DelayTagId))
        {
            return RequestError.InvalidRequest("Choose an active reason.");
        }

        if (options.Crews.All(c => c.Id != draft.OwnerCrewId))
        {
            return RequestError.InvalidRequest("Choose a responsible crew in this project.");
        }

        if (options.Segments.All(s => s.Id != draft.SegmentId))
        {
            return RequestError.InvalidRequest("Choose a segment in this project.");
        }

        if (ValidateImpacts(draft, options) is { } invalidImpact)
        {
            return invalidImpact;
        }

        // The bundled site image has no database map record; its pin still persists.
        if (draft.SiteMapId is not null && draft.SiteMapId != options.SiteMap?.Id)
        {
            return RequestError.InvalidRequest("Choose the active map in this project.");
        }

        if (PhotoRules.Validate(draft.Photos, "A delay report needs one to five photos.") is { } invalidPhoto)
        {
            return invalidPhoto;
        }

        if (draft.NotifyUserIds.Any(id => options.People.All(p => p.UserId != id)))
        {
            return RequestError.InvalidRequest("Notification recipients must belong to this project.");
        }

        return null;
    }

    private static RequestError? ValidateImpacts(RaiseDelayDraft draft, DelayReportOptionsRecord options)
    {
        if (draft.Impacts.Count == 0)
        {
            return RequestError.InvalidRequest("Choose at least one affected crew.");
        }

        if (draft.Impacts.DistinctBy(i => (i.CrewId, i.WorkItemId)).Count() != draft.Impacts.Count)
        {
            return RequestError.InvalidRequest("Each affected crew and work item must be listed once.");
        }

        foreach (var impact in draft.Impacts)
        {
            if (impact.CrewId == draft.OwnerCrewId)
            {
                return RequestError.InvalidRequest("The responsible crew cannot block itself.");
            }

            if (options.Crews.All(c => c.Id != impact.CrewId))
            {
                return RequestError.InvalidRequest("Affected crews must belong to this project.");
            }

            if (ValidateWorkItem(impact.WorkItemId, impact.CrewId, options) is { } invalidWorkItem)
            {
                return invalidWorkItem;
            }
        }

        return ValidateWorkItem(draft.BlockingWorkItemId, draft.OwnerCrewId, options);
    }

    private static RequestError? ValidateWorkItem(int? workItemId, int crewId, DelayReportOptionsRecord options)
    {
        if (workItemId is null || options.WorkItems.Any(w => w.Id == workItemId && w.CrewIds.Contains(crewId)))
        {
            return null;
        }

        return RequestError.InvalidRequest("Choose an unfinished work item assigned to that crew in this project.");
    }

    private static Result<ReportCrewRecord> FindReporterCrew(int userId, RaiseDelayDraft draft, DelayReportOptionsRecord options)
    {
        var crew = options.Crews.SingleOrDefault(c => c.ForemanId == userId);
        if (crew is null)
        {
            return RequestError.Forbidden("You must lead an active crew in this project.");
        }

        if (crew.Id != draft.OwnerCrewId && !draft.Impacts.Any(i => i.CrewId == crew.Id))
        {
            return RequestError.Forbidden("Name your own crew as responsible or affected.");
        }

        return crew;
    }

    private static string LocationSource(RaiseDelayDraft draft)
    {
        if (draft.PinX is not null) return DelayLocationSources.ManualPin;
        return string.IsNullOrWhiteSpace(draft.LocationName) ? DelayLocationSources.PhotoGps : DelayLocationSources.ManualEntry;
    }

    /// <summary>The responsible Superintendent always hears; an advance notice also tells the affected crews' Superintendents.</summary>
    private static IReadOnlyList<int> Recipients(RaiseDelayDraft draft, DelayReportOptionsRecord options)
    {
        var owner = options.Crews.Single(c => c.Id == draft.OwnerCrewId);
        var result = draft.NotifyUserIds.Append(owner.SuperintendentId);
        if (IsAdvanceNotice(draft.ReportType))
        {
            result = result.Concat(options.Crews.Where(c => draft.Impacts.Any(i => i.CrewId == c.Id)).Select(c => c.SuperintendentId));
        }

        // TODO(human): workflow steps never notify the actor (see NotifyCrews); decide whether a report should follow the same rule.
        return result.Distinct().ToList();
    }

    #endregion
}
