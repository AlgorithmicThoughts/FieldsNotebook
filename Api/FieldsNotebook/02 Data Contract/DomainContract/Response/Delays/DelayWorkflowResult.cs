namespace DataContract.Domain.Delays;

/// <summary>What the committed workflow write produced.</summary>
public sealed record DelayWorkflowResult(
    int CycleId,
    int ActivityEventId,
    int NoticeCount);
