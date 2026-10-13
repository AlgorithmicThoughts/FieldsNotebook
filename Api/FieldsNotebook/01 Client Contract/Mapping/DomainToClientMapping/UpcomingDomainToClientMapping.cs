using DataContract.Domain.Insights;
using DataContract.Upcoming;

namespace DataContract.Mapping.DomainToClientMapping;

public static class UpcomingDomainToClientMapping
{
    public static UpcomingResponse ToResponse(this IEnumerable<BlockedWorkView>? blockedWork)
    {
        return new UpcomingResponse(blockedWork.MapAll(ToResponse));
    }

    public static BlockedWorkResponse ToResponse(this BlockedWorkView row)
    {
        return new BlockedWorkResponse(
            row.Impact.ImpactId,
            row.WorkItem.Title,
            row.WorkItem.SegmentName,
            row.Impact.BlockedCrewName,
            row.WorkItem.PlannedStartAt,
            row.Delay.Number,
            row.Delay.Title,
            row.Delay.OwnerCrewName,
            row.Delay.TagName,
            row.IsAdvanceNotice,
            row.IsHighPriority,
            new CompletionEstimateResponse(row.Completion.Source, row.Completion.At, row.Completion.WorkItemTitle),
            row.BlockedPredecessor is { } predecessor ? new BlockedPredecessorResponse(predecessor.Title, predecessor.DelayNumber) : null);
    }
}
