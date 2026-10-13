namespace Common.Errors;

/// <summary>Every expected failure of the delay workflow, worded once.</summary>
public static class DelayErrors
{
    public static RequestError NotFound(int delayId) =>
        RequestError.NotFound($"Delay {delayId} is not available in this project.");

    public static RequestError ImpactNotFound(int delayId, int impactId) =>
        RequestError.NotFound($"Affected crew {impactId} is not on delay {delayId}.");

    /// <param name="action">Completes "The user cannot {action} delay {id}."</param>
    public static RequestError NotPermitted(int delayId, string action) =>
        RequestError.Forbidden($"The user cannot {action} delay {delayId}.");

    public static RequestError StateConflict(int delayId, string reason) =>
        RequestError.Conflict($"Delay {delayId} cannot change: {reason}");
}
