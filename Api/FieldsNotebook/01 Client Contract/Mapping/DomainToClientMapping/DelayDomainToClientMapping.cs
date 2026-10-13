using Common.Delays;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Insights;
using DataContract.Domain.Projects;
using DataContract.Home;

namespace DataContract.Mapping.DomainToClientMapping;

public static class DelayDomainToClientMapping
{
    public static DelayReportOptionsResponse ToResponse(this DelayReportOptionsRecord options, ProjectAccessRecord access)
    {
        return new DelayReportOptionsResponse(access.ProjectId, access.ProjectName,
            options.Crews.MapAll(ToResponse),
            options.WorkItems.MapAll(ToResponse),
            options.Segments.MapAll(ToResponse),
            options.Tags.MapAll(ToResponse),
            options.SiteMap?.ToResponse(),
            options.People.MapAll(ToResponse));
    }

    public static ReportCrewResponse ToResponse(this ReportCrewRecord crew)
    {
        return new ReportCrewResponse(crew.Id, crew.Name);
    }

    public static ReportWorkItemResponse ToResponse(this ReportWorkItemRecord workItem)
    {
        return new ReportWorkItemResponse(workItem.Id, workItem.Title, workItem.CrewIds.MapAll(id => id));
    }

    public static ReportSegmentResponse ToResponse(this ReportSegmentRecord segment)
    {
        return new ReportSegmentResponse(segment.Id, segment.Name);
    }

    public static ReportTagResponse ToResponse(this ReportTagRecord tag)
    {
        return new ReportTagResponse(tag.Id, tag.Code, tag.Name);
    }

    public static ReportPersonResponse ToResponse(this ReportPersonRecord person)
    {
        return new ReportPersonResponse(person.UserId, person.DisplayName, person.CrewId, person.CrewName, person.RoleName);
    }

    public static TrackingSiteMapResponse ToResponse(this ReportMapRecord map)
    {
        return new TrackingSiteMapResponse(map.Id, map.Name, map.Uri);
    }

    public static TrackingSiteMapResponse ToResponse(this TrackingSiteMapRecord map)
    {
        return new TrackingSiteMapResponse(map.Id, map.Name, map.Uri);
    }

    public static DelayCycleResponse ToResponse(this TrackingCycleRecord cycle, int sequence)
    {
        return new DelayCycleResponse(cycle.Id, sequence, cycle.CreatedAt, cycle.OpenReason, cycle.AcknowledgmentChoice,
            cycle.AcknowledgmentNote, cycle.AcknowledgedAt, cycle.AcknowledgedByName, cycle.ExpectedReadyAt, cycle.ClosedAt);
    }

    public static DelayVerificationResponse ToResponse(this TrackingVerificationRecord verification)
    {
        return new DelayVerificationResponse(verification.VerifiedByName, verification.VerifiedAt, verification.Note);
    }

    /// <param name="readUrl">Turns a stored blob URL into one the client can open.</param>
    public static DelayPhotoResponse ToResponse(this TrackingPhotoRecord photo, Func<string, string> readUrl)
    {
        return new DelayPhotoResponse(photo.Id, readUrl(photo.OriginalUrl), readUrl(photo.ThumbnailUrl), photo.FileName,
            photo.CreatedAt, photo.Latitude, photo.Longitude);
    }

    public static DelayEventResponse ToResponse(this TrackingEventRecord activityEvent, int? cycleSequence)
    {
        return new DelayEventResponse(activityEvent.Id, activityEvent.EventType, activityEvent.ActorName, activityEvent.CreatedAt,
            cycleSequence, activityEvent.OldExpectedReadyAt, activityEvent.NewExpectedReadyAt);
    }

    public static DelayImpactResponse ToResponse(this TrackingImpactRecord impact)
    {
        return new DelayImpactResponse(impact.Id, impact.CrewName, impact.WorkItemId);
    }

    public static DelayImpactResponse ToResponse(this ActiveDelayImpactRecord impact)
    {
        return new DelayImpactResponse(impact.ImpactId, impact.BlockedCrewName, impact.BlockedWorkItemId);
    }

    /// <summary>One delay as a list row shows it. Each affected crew is named once, however many work items it has.</summary>
    /// <param name="isHighPriority">Decided by the delay rules, not by the mapping.</param>
    public static DelaySummaryResponse ToSummaryResponse(this ActiveDelayRecord delay, IEnumerable<DelayImpactResponse>? impacts,
        ProjectAccessRecord project, bool isHighPriority)
    {
        var impactResponses = impacts.MapAll(i => i);
        return new DelaySummaryResponse(delay.DelayId, delay.Number, delay.Title, delay.StatusCode, delay.TagCode, delay.TagName,
            delay.OwnerCrewName, impactResponses.Select(i => i.CrewName).Distinct().ToList(), impactResponses,
            delay.LocationName, project.ProjectId, project.ProjectName, isHighPriority, delay.ReportType, delay.CreatedAt,
            delay.ExpectedReadyAt, ToPinResponse(delay.PinX, delay.PinY), delay.SiteMapId, delay.SegmentName);
    }

    /// <summary>One active delay as a list row shows it, with the priority the insight Engine decided.</summary>
    public static DelaySummaryResponse ToSummaryResponse(this DelaySummaryView summary, ProjectAccessRecord project)
    {
        return summary.Delay.ToSummaryResponse(summary.Impacts.MapAll(ToResponse), project, summary.IsHighPriority);
    }

    /// <param name="readUrl">Turns a stored photo URL into one the client can open.</param>
    public static TrackingResponse ToTrackingResponse(this IEnumerable<TrackingDelayView>? delays, ProjectAccessRecord access,
        Func<string, string> readUrl)
    {
        return new TrackingResponse(access.ProjectId, access.ProjectName, delays.MapAll(d => d.ToResponse(access, readUrl)));
    }

    /// <param name="readUrl">Turns a stored photo URL into one the client can open.</param>
    public static TrackingDelayResponse ToResponse(this TrackingDelayView view, ProjectAccessRecord access, Func<string, string> readUrl)
    {
        var delay = view.Delay.Summary;
        return new TrackingDelayResponse(
            delay.ToSummaryResponse(view.Impacts.MapAll(i => i.Impact.ToResponse()), access, view.IsHighPriority),
            delay.Description,
            new DelayReporterResponse(view.Delay.ReporterId, view.Delay.ReporterName, view.Delay.ReporterCrewName, delay.CreatedAt),
            view.Cycles.MapAll(c => c.Cycle.ToResponse(c.Sequence)),
            view.Photos.MapAll(g => new DelayPhotoGroupResponse(g.Purpose, g.Photos.MapAll(p => p.ToResponse(readUrl)))),
            view.Impacts.MapAll(i => new TrackingImpactResponse(i.Impact.Id, i.Impact.CrewName, i.Impact.WorkItemId, i.Impact.WorkItemTitle,
                i.CurrentCycleVerifications.MapAll(ToResponse))),
            view.Events.MapAll(e => e.Event.ToResponse(e.CycleSequence)),
            view.SiteMap?.ToResponse());
    }

    public static DelaySpeechResponse ToResponse(this ReportSuggestionRecord suggestion)
    {
        return new DelaySpeechResponse(suggestion.Title, suggestion.Description, suggestion.OwnerCrewId, suggestion.DelayTagId);
    }

    /// <summary>The list row of a delay just raised, built from the write and the options it was checked against.</summary>
    public static DelaySummaryResponse ToSummaryResponse(this RaiseDelayWrite report, ReportSavedRecord saved,
        ProjectAccessRecord access, DelayReportOptionsRecord options)
    {
        var crewNames = options.Crews.MapAll(c => c).ToDictionary(c => c.Id, c => c.Name);
        var tag = options.Tags.Single(t => t.Id == report.DelayTagId);
        var segment = options.Segments.Single(s => s.Id == report.SegmentId);
        var impacts = report.Impacts.MapAll(i => i);

        return new DelaySummaryResponse(saved.Id, saved.Number, report.Title, DelayStatusCodes.Raised, tag.Code, tag.Name,
            crewNames[report.OwnerCrewId], impacts.Select(i => crewNames[i.CrewId]).Distinct().ToList(),
            impacts.Select((i, index) => new DelayImpactResponse(saved.ImpactIds[index], crewNames[i.CrewId], i.WorkItemId)).ToList(),
            string.IsNullOrWhiteSpace(report.LocationName) ? segment.Name : report.LocationName, access.ProjectId,
            access.ProjectName, IsHighPriority: false, report.ReportType, report.CreatedAt, ExpectedReadyAt: null,
            ToPinResponse(report.PinX, report.PinY), report.SiteMapId, segment.Name);
    }

    /// <summary>The outcome of an action, described by its last step.</summary>
    public static DelayActionResponse ToResponse(this DelayWorkflowWrite write, DelayWorkflowResult result)
    {
        return new DelayActionResponse(write.DelayId, write.ResultingStatusCode, result.CycleId, result.ActivityEventId,
            write.EventType, write.OccurredAt, result.NoticeCount);
    }

    public static DelayPinResponse? ToPinResponse(decimal? x, decimal? y)
    {
        if (x is { } pinX && y is { } pinY)
        {
            return new DelayPinResponse(pinX, pinY);
        }

        return null;
    }
}
