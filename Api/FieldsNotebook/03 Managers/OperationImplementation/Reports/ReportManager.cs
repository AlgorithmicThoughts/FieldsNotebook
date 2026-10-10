using Accessors.Delays;
using Accessors.Projects;
using Common.Errors;
using Common.Reports;
using DataContract.Reports;

namespace Managers.Reports;

public class ReportManager(IProjectAccessAccessor projectAccessAccessor, IDelayAccessor delayAccessor) : IReportManager
{
    public async Task<IReadOnlyList<DelayReportRowResponse>> GetDelayReportAsync(
        int userId, DelayReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
        var projectIds = ValidatedProjectIds(request);

        // Sequential on purpose: every call shares this request's DbContext.
        // A report must not widen past the projects the user can open, so one denial fails the whole scope.
        foreach (var projectId in projectIds)
        {
            _ = await projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken)
                ?? throw new ProjectAccessDeniedException(projectId);
        }

        var data = await delayAccessor.GetDelayReportAsync(
            projectIds, StartOfUtcDay(request.DateFrom), StartOfUtcDay(request.DateTo?.AddDays(1)), cancellationToken);
        var blockedCrews = data.Impacts.ToLookup(i => i.DelayId, i => i.CrewName);

        return data.Delays.Select(d => ToRow(d, blockedCrews[d.DelayId].ToList())).ToList();
    }

    public Task<IReadOnlyList<string>> GetDelayTagNamesAsync(int userId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
        return delayAccessor.GetActiveDelayTagNamesAsync(cancellationToken);
    }

    /// <summary>
    /// A report without a project would expose every project's delays, so an empty scope fails rather than widening.
    /// </summary>
    private static IReadOnlyList<int> ValidatedProjectIds(DelayReportRequest request)
    {
        if (request.ProjectIds is not { Length: > 0 } ids)
        {
            throw new InvalidRequestException("A delay report needs at least one project.");
        }

        if (request.DateFrom > request.DateTo)
        {
            throw new InvalidRequestException("The start date must be on or before the end date.");
        }

        foreach (var id in ids)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(id, 1);
        }

        return ids.Distinct().ToList();
    }

    private static DateTimeOffset? StartOfUtcDay(DateOnly? day) =>
        day is { } value ? new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    private static DelayReportRowResponse ToRow(DelayReportRecord d, IReadOnlyList<string> blockedCrewNames) =>
        new(d.Number, d.ProjectName, d.Title, d.StatusName, d.TagName, d.ReportType, d.OwnerCrewName, blockedCrewNames,
            d.SegmentName, d.LocationName, d.ReportedByName, d.CreatedAt, d.FinalCycleClosedAt, d.CycleCount, d.PhotoCount);
}
