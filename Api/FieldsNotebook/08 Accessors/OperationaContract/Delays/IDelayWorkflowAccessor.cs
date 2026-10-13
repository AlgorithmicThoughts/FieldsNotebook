using Common.Results;
using DataContract.Domain.Delays;

namespace Accessors.Delays
{
    public interface IDelayWorkflowAccessor
    {
        /// <summary>
        /// The delay's current status, its latest cycle, and every impact with that cycle's handover
        /// checks. Returns null when the delay does not belong to the project.
        /// </summary>
        Task<DelayWorkflowState?> GetWorkflowStateAsync(int projectId, int delayId, CancellationToken cancellationToken);

        /// <summary>
        /// Applies every step of one workflow action in a single transaction: for each step, the status
        /// move, the cycle change, an optional handover check, any proof-of-work photos, the activity event,
        /// and one notice per Foreman and Superintendent of the recipient crews, never the actor.
        /// A step is refused when the delay is no longer in the status it names, so a concurrent action
        /// cannot be silently overwritten; the refusal comes back as a conflict, not an exception.
        /// Uploaded photos are removed again when the transaction fails.
        /// </summary>
        /// <returns>What the last step produced, or a conflict when a step was refused and nothing was written.</returns>
        Task<Result<DelayWorkflowResult>> ApplyAsync(IReadOnlyList<DelayWorkflowWrite> writes, CancellationToken cancellationToken);
    }
}
