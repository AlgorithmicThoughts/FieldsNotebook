using DataContract.Domain.Projects;

namespace DataContract.Domain.Delays;

/// <summary>
/// Everything the workflow Engine decides one action against: who acts, their access, the delay's state,
/// and the crew contacts it may notify.
/// </summary>
public sealed record DelayWorkflowContext(
    int ActorUserId,
    ProjectAccessRecord Access,
    DelayWorkflowState State,
    DateTimeOffset OccurredAt,
    IReadOnlyList<CrewContactRecord> CrewContacts);
