using Common.Delays;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Delays
{
    public class DelayAccessor(FieldNotebookContext context) : IDelayAccessor
    {
        public async Task<ActiveDelaySet> GetActiveDelaysForCrewsAsync(
            int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken)
        {
            var delays = await (
                from d in context.Delays.AsNoTracking()
                join s in context.DelayStatus.AsNoTracking() on d.DelayStatusId equals s.Id
                join t in context.DelayTags.AsNoTracking() on d.DelayTagId equals t.Id
                join owner in context.Crew.AsNoTracking() on d.OwnerCrewId equals owner.Id
                join segment in context.Segments.AsNoTracking() on d.SegmentId equals segment.Id
                where d.ProjectId == projectId
                      && s.Code != DelayStatusCodes.Resolved && s.Code != DelayStatusCodes.Revoked
                      && (crewIds.Contains(d.OwnerCrewId)
                          || context.DelayImpacts.AsNoTracking().Any(i => i.DelayId == d.Id && crewIds.Contains(i.BlockedCrewId)))
                orderby d.CreatedAt descending, d.Id descending
                select new ActiveDelayRecord(
                    d.Id,
                    d.Number,
                    d.Title,
                    d.Description,
                    s.Code,
                    t.Code,
                    t.Name,
                    d.OwnerCrewId,
                    owner.Name,
                    d.ReportType,
                    d.LocationName ?? segment.Name,
                    segment.Name,
                    d.SiteMapId,
                    d.PinX,
                    d.PinY,
                    d.CreatedAt,
                    context.DelayCycles.AsNoTracking().Count(c => c.DelayId == d.Id),
                    context.DelayCycles.AsNoTracking().Any(c => c.DelayId == d.Id && c.ClosedAt == null),
                    context.DelayCycles.AsNoTracking()
                        .Where(c => c.DelayId == d.Id && c.ClosedAt == null)
                        .Select(c => c.ExpectedReadyAt)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken);
            if (delays.Count == 0)
            {
                return ActiveDelaySet.Empty;
            }

            var delayIds = delays.Select(d => d.DelayId).ToList();
            var impacts = await (
                from i in context.DelayImpacts.AsNoTracking()
                join crew in context.Crew.AsNoTracking() on i.BlockedCrewId equals crew.Id
                where delayIds.Contains(i.DelayId)
                orderby crew.Name
                select new ActiveDelayImpactRecord(
                    i.Id,
                    i.DelayId,
                    i.BlockedCrewId,
                    crew.Name,
                    i.BlockedWorkItemId,
                    context.DelayImpactVerifications.AsNoTracking().Any(v =>
                        v.DelayImpactId == i.Id
                        && context.DelayCycles.AsNoTracking().Any(c => c.DelayCycleId == v.DelayCycleId && c.ClosedAt == null))))
                .ToListAsync(cancellationToken);

            return new ActiveDelaySet(delays, impacts);
        }
    }
}
