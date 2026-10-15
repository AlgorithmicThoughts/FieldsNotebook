using Common.Errors;
using Common.Results;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;

namespace Engines.Delays;

/// <summary>
/// Who may move a delay and when, from the Foreman's report to its resolution. Each action returns
/// either the writes that carry it out or the reason it is refused; nothing here reaches a Resource.
/// </summary>
public interface IDelayWorkflowEngine : IEngine
{
    /// <summary>Only a Foreman reports a delay, so only a Foreman reads the report form or transcribes one.</summary>
    /// <returns>The refusal, or null when the user may report.</returns>
    RequestError? CheckReporter(ProjectAccessRecord access);

    /// <summary>Checks the report against the project's options and returns the write that raises it.</summary>
    Result<RaiseDelayWrite> Raise(int actorUserId, int projectId, RaiseDelayDraft draft, DelayReportOptionsRecord options,
        DateTimeOffset createdAt);

    /// <summary>The responsible Superintendent answers the delay and may give a tentative completion time.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> Acknowledge(DelayWorkflowContext context, DelayAcknowledgmentChange acknowledgment,
        DateTimeOffset? expectedReadyAt);

    /// <summary>The responsible Superintendent revises the tentative completion time of the active cycle.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> ReviseCompletionTime(DelayWorkflowContext context, DateTimeOffset? expectedReadyAt);

    /// <summary>
    /// The responsible crew's Superintendent or Foreman submits the delay for review, proving the work with photos.
    /// The affected crews that have not accepted yet are asked to review it.
    /// </summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> RequestHandoverCheck(DelayWorkflowContext context, IReadOnlyList<ReportPhotoUploadRecord> proofOfWork);

    /// <summary>
    /// An affected crew's Foreman accepts the proof of work for their own crew only. The last crew to accept
    /// closes the delay, which notifies every crew on it.
    /// </summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> VerifyHandover(DelayWorkflowContext context, DelayVerificationChange verification);

    /// <summary>An affected crew's Foreman sends the proof of work back with a reason, returning the delay to Acknowledged.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> ReturnHandover(DelayWorkflowContext context, DelayReturnChange handoverReturn);

    /// <summary>A Superintendent reopens a resolved delay, which opens a new cycle for the same delay.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> Reopen(DelayWorkflowContext context, string reason);

    /// <summary>The Superintendent of the reporting Foreman withdraws the delay.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> Revoke(DelayWorkflowContext context);
}
