using Common.Activity;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Activity
{
    public class ActivityAccessor(FieldNotebookContext context) : IActivityAccessor
    {
        public async Task<IReadOnlyList<ActivityEventRecord>> GetRecentActivityForCrewsAsync(
            int projectId, IReadOnlyCollection<int> crewIds, int limit, CancellationToken cancellationToken) =>
            await (
                from e in context.ActivityEvents
                join d in context.Delays on e.DelayId equals d.Id
                join actor in context.Users on e.ActorUserId equals actor.Id
                where e.ProjectId == projectId
                      && (crewIds.Contains(d.OwnerCrewId)
                          || context.DelayImpacts.Any(i => i.DelayId == d.Id && crewIds.Contains(i.BlockedCrewId)))
                orderby e.CreatedAt descending, e.Id descending
                select new ActivityEventRecord(
                    e.Id,
                    e.EventType,
                    d.Number,
                    d.Title,
                    actor.DisplayName,
                    context.DelayStatus.Where(s => s.Id == e.FromDelayStatusId).Select(s => s.Code).FirstOrDefault(),
                    context.DelayStatus.Where(s => s.Id == e.ToDelayStatusId).Select(s => s.Code).FirstOrDefault(),
                    e.NewExpectedReadyAt,
                    (from i in context.DelayImpacts
                     join crew in context.Crew on i.BlockedCrewId equals crew.Id
                     where i.Id == e.DelayImpactId
                     select crew.Name).FirstOrDefault(),
                    e.CreatedAt))
                .Take(limit)
                .ToListAsync(cancellationToken);
    }
}
