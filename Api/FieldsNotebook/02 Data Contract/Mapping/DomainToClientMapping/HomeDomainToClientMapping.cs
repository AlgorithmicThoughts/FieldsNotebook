using DataContract.Domain.Activity;
using DataContract.Home;

namespace DataContract.Mapping.DomainToClientMapping;

public static class HomeDomainToClientMapping
{
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
