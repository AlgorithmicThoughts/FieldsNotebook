using Common.WorkItems;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.WorkItems
{
    public class WorkItemAccessor(FieldNotebookContext context) : IWorkItemAccessor
    {
        public async Task<PlannedWorkSet> GetPlannedWorkAsync(
            int projectId, DateTimeOffset plannedBeforeUtc, CancellationToken cancellationToken)
        {
            var workItems = await (
                from w in context.WorkItems
                join segment in context.Segments on w.SegmentId equals segment.Id
                where w.ProjectId == projectId && !w.IsCompleted && w.PlannedStartAt < plannedBeforeUtc
                orderby w.PlannedStartAt, w.Id
                select new PlannedWorkItemRecord(w.Id, w.Title, segment.Name, w.PlannedStartAt, w.ExpectedFinishAt))
                .ToListAsync(cancellationToken);
            if (workItems.Count == 0)
            {
                return PlannedWorkSet.Empty;
            }

            var workItemIds = workItems.Select(w => w.WorkItemId).ToList();
            var predecessors = await (
                from d in context.WorkItemDependencies
                join p in context.WorkItems on d.PredecessorWorkItemId equals p.Id
                where d.ProjectId == projectId && workItemIds.Contains(d.SuccessorWorkItemId)
                orderby d.SuccessorWorkItemId, p.Id
                select new WorkItemPredecessorRecord(d.SuccessorWorkItemId, p.Id, p.Title, p.ExpectedFinishAt))
                .ToListAsync(cancellationToken);

            return new PlannedWorkSet(workItems, predecessors);
        }
    }
}
