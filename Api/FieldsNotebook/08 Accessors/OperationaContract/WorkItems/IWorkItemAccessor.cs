using DataContract.Domain.WorkItems;

namespace Accessors.WorkItems
{
    public interface IWorkItemAccessor
    {
        /// <summary>
        /// Unfinished work in the project planned to start before the given instant, earliest first,
        /// with the predecessors of those work items.
        /// </summary>
        /// <param name="plannedBeforeUtc">Exclusive upper bound on WorkItems.PlannedStartAt, in UTC.</param>
        Task<PlannedWorkSet> GetPlannedWorkAsync(int projectId, DateTimeOffset plannedBeforeUtc, CancellationToken cancellationToken);
    }
}
