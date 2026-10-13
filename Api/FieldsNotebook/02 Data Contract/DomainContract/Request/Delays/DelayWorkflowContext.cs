using DataContract.Domain.Projects;

namespace DataContract.Domain.Delays;

/// <summary>Everything the workflow Engine decides one action against: who acts, their access, and the delay's state.</summary>
public sealed record DelayWorkflowContext(
    int ActorUserId,
    ProjectAccessRecord Access,
    DelayWorkflowState State,
    DateTimeOffset OccurredAt);
