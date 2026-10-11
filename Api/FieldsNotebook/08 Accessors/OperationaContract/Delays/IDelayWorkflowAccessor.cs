using Common.Delays;

namespace Accessors.Delays
{
    public interface IDelayWorkflowAccessor
    {
        /// <summary>
        /// The delay's current status, its latest cycle, and every impact with that cycle's handover
        /// checks. Returns null when the delay does not belong to the project.
        /// </summary>
        Task<DelayWorkflowState?> GetWorkflowStateAsync(int projectId, int delayId, CancellationToken cancellationToken);

        /// <summary>DelayStatus.Id for a status code, or null when the code is not an active status.</summary>
        Task<int?> GetStatusIdAsync(string statusCode, CancellationToken cancellationToken);

        /// <summary>
        /// The active Foremen and Superintendents of the given crews who hold access to the project,
        /// as distinct user IDs. This is the recipient pool for a delay notice.
        /// </summary>
        Task<IReadOnlyList<int>> GetCrewContactsAsync(int projectId, IReadOnlyCollection<int> crewIds, CancellationToken cancellationToken);

        /// <summary>
        /// Applies one workflow action in a single transaction: the status move, the cycle change, an
        /// optional handover check, the activity event, and one notice per recipient.
        /// </summary>
        Task<DelayWorkflowResult> ApplyAsync(DelayWorkflowWrite write, CancellationToken cancellationToken);
    }
}
