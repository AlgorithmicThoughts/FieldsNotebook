using DataContract.Domain.Activity;
using DataContract.Domain.Insights;
using DataContract.Domain.Projects;
using DataContract.Home;

namespace DataContract.Mapping.DomainToClientMapping;

public static class HomeDomainToClientMapping
{
    public static HomeResponse ToResponse(this HomeView home, ProjectAccessRecord access)
    {
        return new HomeResponse(
            new HomeMetricsResponse(home.Metrics.BlockingOurCrews, home.Metrics.CrewsWaitingOnUs, home.Metrics.ReadyToVerify),
            home.Attention.MapAll(d => d.ToSummaryResponse(access)),
            home.AdvanceNotice?.ToSummaryResponse(access),
            home.RecentActivity.ToResponse());
    }

    public static ActivityEntryResponse ToResponse(this ActivityEventRecord activityEvent)
    {
        return new ActivityEntryResponse(activityEvent.Id, activityEvent.EventType, activityEvent.DelayNumber,
            activityEvent.DelayTitle, activityEvent.ActorName, activityEvent.FromStatusCode, activityEvent.ToStatusCode,
            activityEvent.NewExpectedReadyAt, activityEvent.AffectedCrewName, activityEvent.CreatedAt);
    }

    public static IReadOnlyList<ActivityEntryResponse> ToResponse(this IEnumerable<ActivityEventRecord>? activity)
    {
        return activity.MapAll(ToResponse);
    }
}
