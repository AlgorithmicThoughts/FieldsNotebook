namespace DataContract.Upcoming;

/// <summary>
/// The tentative completion the row shows, and which field it was read from. The two sources mean
/// different things, so the row says which one it is. Neither clears the blocker.
/// </summary>
/// <param name="Source">A value of <see cref="Common.Delays.CompletionEstimateSources"/>.</param>
/// <param name="WorkItemTitle">The predecessor the estimate came from, for the workItem source only.</param>
public sealed record CompletionEstimateResponse(
    string Source,
    DateTimeOffset? At,
    string? WorkItemTitle);
