using Common.Results;
using DataContract.Delays;
using DataContract.Home;

namespace Managers.Delays;

/// <summary>
/// The delay workflow from the Foreman's report to its resolution.
/// Expected failures come back in the <see cref="Result{T}"/>; only unexpected ones throw.
/// </summary>
public interface IDelayManager : IManager
{
    /// <summary>Everything the Foreman's report form offers for the project.</summary>
    Task<Result<DelayReportOptionsResponse>> GetReportOptionsAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>Suggests report fields from a spoken description. Nothing is saved.</summary>
    Task<Result<DelaySpeechResponse>> TranscribeReportAsync(int userId, int projectId, DelaySpeechRequest request, CancellationToken cancellationToken);

    /// <summary>A Foreman raises a delay against a responsible crew, naming every affected crew.</summary>
    Task<Result<DelaySummaryResponse>> RaiseAsync(int userId, int projectId, RaiseDelayRequest request, CancellationToken cancellationToken);

    /// <summary>The responsible Superintendent answers the delay and gives a tentative completion time.</summary>
    Task<Result<DelayActionResponse>> AcknowledgeAsync(int userId, int projectId, int delayId, AcknowledgeDelayRequest request, CancellationToken cancellationToken);

    /// <summary>The responsible Superintendent revises the tentative completion time of the active cycle.</summary>
    Task<Result<DelayActionResponse>> ReviseCompletionTimeAsync(int userId, int projectId, int delayId, ReviseCompletionTimeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// The responsible crew's Superintendent or Foreman submits the delay for review, attaching proof-of-work
    /// photos that are stored against the open cycle.
    /// </summary>
    Task<Result<DelayActionResponse>> RequestHandoverCheckAsync(int userId, int projectId, int delayId, RequestHandoverCheckRequest request, CancellationToken cancellationToken);

    /// <summary>An affected crew's Foreman accepts the proof of work for their crew; the last acceptance closes the delay.</summary>
    Task<Result<DelayActionResponse>> VerifyHandoverAsync(int userId, int projectId, int delayId, int impactId, VerifyHandoverRequest request, CancellationToken cancellationToken);

    /// <summary>An affected crew's Foreman sends the proof of work back, saying what the responsible crew must fix.</summary>
    Task<Result<DelayActionResponse>> ReturnHandoverAsync(int userId, int projectId, int delayId, int impactId, ReturnHandoverRequest request, CancellationToken cancellationToken);

    /// <summary>A Superintendent reopens a resolved delay, which opens a new cycle for the same delay.</summary>
    Task<Result<DelayActionResponse>> ReopenAsync(int userId, int projectId, int delayId, ReopenDelayRequest request, CancellationToken cancellationToken);

    /// <summary>The Superintendent of the reporting Foreman withdraws the delay.</summary>
    Task<Result<DelayActionResponse>> RevokeAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken);
}
