using DataContract.Domain.Activity;

namespace Accessors.Activity
{
    public interface IActivityAccessor
    {
        /// <summary>
        /// Newest events, in any delay status, on delays in the project where one of the crews is responsible or affected.
        /// </summary>
        Task<IReadOnlyList<ActivityEventRecord>> GetRecentActivityForCrewsAsync(
            int projectId, IReadOnlyCollection<int> crewIds, int limit, CancellationToken cancellationToken);
    }
}
