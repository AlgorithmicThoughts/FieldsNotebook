using DataContract.Delays;

namespace Managers.Delays;

public interface IDelayManager
{
    Task<TrackingResponse> GetProjectTrackingDetailsAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>The responsible Superintendent answers the delay and gives a tentative completion time.</summary>
    Task<DelayActionResponse> AcknowledgeAsync(int userId, int projectId, int delayId, AcknowledgeDelayRequest request, CancellationToken cancellationToken);

    /// <summary>The responsible Superintendent revises the tentative completion time of the active cycle.</summary>
    Task<DelayActionResponse> ReviseCompletionTimeAsync(int userId, int projectId, int delayId, ReviseCompletionTimeRequest request, CancellationToken cancellationToken);

    /// <summary>The responsible Superintendent asks the affected crews' Foremen for a handover check.</summary>
    Task<DelayActionResponse> RequestHandoverCheckAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken);

    /// <summary>The affected crew's Foreman records a successful handover check for the current cycle.</summary>
    Task<DelayActionResponse> VerifyHandoverAsync(int userId, int projectId, int delayId, VerifyHandoverRequest request, CancellationToken cancellationToken);

    /// <summary>The responsible Superintendent closes the cycle after the required handover checks.</summary>
    Task<DelayActionResponse> ResolveAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken);

    /// <summary>A Superintendent reopens a resolved delay, which opens a new cycle for the same delay.</summary>
    Task<DelayActionResponse> ReopenAsync(int userId, int projectId, int delayId, ReopenDelayRequest request, CancellationToken cancellationToken);

    /// <summary>The Superintendent of the reporting Foreman withdraws the delay.</summary>
    Task<DelayActionResponse> RevokeAsync(int userId, int projectId, int delayId, CancellationToken cancellationToken);
}
