namespace DataContract.Domain.Delays;

/// <summary>
/// One step of a delay workflow action, decided by the workflow Engine and applied by the workflow
/// Accessor: the status move, the cycle change, an optional handover check, any proof-of-work photos,
/// the activity event that preserves the step, and one notice per recipient.
/// </summary>
/// <param name="FromStatusCode">The status the Engine validated. The write is refused if the delay has since moved.</param>
/// <param name="ToStatusCode">Null when the step changes no status, such as a revised completion time.</param>
/// <param name="RecipientUserIds">The users this step notifies, decided by the workflow Engine; never the actor.</param>
public sealed record DelayWorkflowWrite(
    int DelayId,
    int ProjectId,
    int ActorUserId,
    DateTimeOffset OccurredAt,
    string EventType,
    string FromStatusCode,
    string? ToStatusCode,
    DelayCycleChange Cycle,
    IReadOnlyList<int> RecipientUserIds)
{
    /// <summary>Only a handover verification carries one.</summary>
    public DelayVerificationChange? Verification { get; init; }

    public DateTimeOffset? OldExpectedReadyAt { get; init; }

    public DateTimeOffset? NewExpectedReadyAt { get; init; }

    /// <summary>
    /// Uploads to store against the cycle with Purpose ProofOfWork. Only a resolution carries them; the Manager
    /// stores them before the write and hands the Accessor the stored copies.
    /// </summary>
    public IReadOnlyList<ReportPhotoUploadRecord> ProofOfWorkPhotos { get; init; } = [];

    /// <summary>Where the delay stands once this step is applied.</summary>
    public string ResultingStatusCode => ToStatusCode ?? FromStatusCode;
}
