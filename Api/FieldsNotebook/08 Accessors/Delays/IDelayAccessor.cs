using Common.Delays;

namespace Accessors.Delays
{
    public interface IDelayAccessor
    {
        /// <summary>
        /// Active delays in the project where one of the crews is responsible or affected, newest first,
        /// with every impact of those delays.
        /// </summary>
        Task<ActiveDelaySet> GetActiveDelaysForCrewsAsync(int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken);
    }
}
