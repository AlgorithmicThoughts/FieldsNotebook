using Common.Results;
using DataContract.Domain.Delays;

namespace Accessors.Delays;

public interface IDelayWorkflowAccessor : IAccessor
{
    /// <summary>
    /// The delay's current status, its latest cycle, and every impact with that cycle's handover
    /// checks. Returns null when the delay does not belong to the project.
    /// </summary>
    Task<DelayWorkflowState?> GetWorkflowStateAsync(int projectId, int delayId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies every step of one workflow action in a single transaction: for each step, the status
    /// move, the cycle change, an optional handover check, the proof-of-work photos, the activity event,
    /// and one notice per recipient the step names.
    /// A step is refused when the delay is no longer in the status it names, so a concurrent action
    /// cannot be silently overwritten; the refusal comes back as a conflict, not an exception.
    /// </summary>
    /// <param name="proofOfWorkPhotos">Already stored; recorded against the step that carries proof-of-work uploads.</param>
    /// <returns>What the last step produced, or a conflict when a step was refused and nothing was written.</returns>
    Task<Result<DelayWorkflowResult>> ApplyAsync(IReadOnlyList<DelayWorkflowWrite> writes, IReadOnlyList<ReportPhotoRecord> proofOfWorkPhotos,
        CancellationToken cancellationToken);
}
