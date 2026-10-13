using DataContract.Domain.Delays;
using DataContract.Domain.WorkItems;

namespace DataContract.Domain.Insights;

/// <summary>One blocked work item planned by the end of the next project day. One delay with several impacts produces several rows.</summary>
public sealed record BlockedWorkView(
    ActiveDelayRecord Delay,
    ActiveDelayImpactRecord Impact,
    PlannedWorkItemRecord WorkItem,
    bool IsAdvanceNotice,
    bool IsHighPriority,
    CompletionEstimateView Completion,
    BlockedPredecessorView? BlockedPredecessor);

/// <param name="Source">A value of <see cref="Common.Delays.CompletionEstimateSources"/>.</param>
/// <param name="WorkItemTitle">The predecessor the estimate came from, for the workItem source only.</param>
public sealed record CompletionEstimateView(
    string Source,
    DateTimeOffset? At,
    string? WorkItemTitle);

/// <summary>A predecessor that an unresolved delay still sits on.</summary>
public sealed record BlockedPredecessorView(
    string Title,
    string DelayNumber);
