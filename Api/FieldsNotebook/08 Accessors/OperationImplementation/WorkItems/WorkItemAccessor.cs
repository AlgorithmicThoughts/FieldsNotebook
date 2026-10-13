using DataContract.Domain.Delays;
using DataContract.Domain.WorkItems;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.WorkItems;

public class WorkItemAccessor(FieldNotebookContext context) : IWorkItemAccessor
{
    public async Task<PlannedWorkSet> GetPlannedWorkAsync(
        int projectId, DateTimeOffset plannedBeforeUtc, CancellationToken cancellationToken)
    {
        var workItems = await (
            from w in context.WorkItems.AsNoTracking()
            join segment in context.Segments.AsNoTracking() on w.SegmentId equals segment.Id
            where w.ProjectId == projectId && w.IsCompleted == false && w.PlannedStartAt < plannedBeforeUtc
            orderby w.PlannedStartAt, w.Id
            select new PlannedWorkItemRecord(w.Id, w.Title, segment.Name, w.PlannedStartAt, w.ExpectedFinishAt))
            .ToListAsync(cancellationToken);
        if (workItems.Count == 0)
        {
            return PlannedWorkSet.Empty;
        }

        var workItemIds = workItems.Select(w => w.WorkItemId).ToList();
        var predecessors = await (
            from d in context.WorkItemDependencies.AsNoTracking()
            join p in context.WorkItems.AsNoTracking() on d.PredecessorWorkItemId equals p.Id
            where d.ProjectId == projectId && workItemIds.Contains(d.SuccessorWorkItemId)
            orderby d.SuccessorWorkItemId, p.Id
            select new WorkItemPredecessorRecord(d.SuccessorWorkItemId, p.Id, p.Title, p.ExpectedFinishAt))
            .ToListAsync(cancellationToken);

        return new PlannedWorkSet(workItems, predecessors);
    }

    public async Task<IReadOnlyList<ReportWorkItemRecord>> GetOpenWorkItemsAsync(int projectId, CancellationToken cancellationToken)
    {
        var items = await context.WorkItems.AsNoTracking().Where(w => w.ProjectId == projectId && w.IsCompleted == false)
            .OrderBy(w => w.Title).Select(w => new { w.Id, w.Title }).ToListAsync(cancellationToken);
        var assignments = await context.WorkItemTeamAssignment.AsNoTracking().Where(a => a.ProjectId == projectId)
            .Select(a => new { a.WorkItemId, a.CrewId }).ToListAsync(cancellationToken);
        var crews = assignments.ToLookup(a => a.WorkItemId, a => a.CrewId);
        return items.Select(w => new ReportWorkItemRecord(w.Id, w.Title, crews[w.Id].ToList())).ToList();
    }
}
