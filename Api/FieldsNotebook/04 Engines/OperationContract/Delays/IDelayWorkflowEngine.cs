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

    /// <summary>The responsible Superintendent asks the affected crews' Foremen for a handover check.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> RequestHandoverCheck(DelayWorkflowContext context);

    /// <summary>The affected crew's Foreman records a successful handover check for the current cycle.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> VerifyHandover(DelayWorkflowContext context, DelayVerificationChange verification);

    /// <summary>The responsible Superintendent closes the cycle after the handover checks, proving the work with photos.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> Resolve(DelayWorkflowContext context, IReadOnlyList<ReportPhotoUploadRecord> proofOfWork);

    /// <summary>A Superintendent reopens a resolved delay, which opens a new cycle for the same delay.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> Reopen(DelayWorkflowContext context, string reason);

    /// <summary>The Superintendent of the reporting Foreman withdraws the delay.</summary>
    Result<IReadOnlyList<DelayWorkflowWrite>> Revoke(DelayWorkflowContext context);
}
