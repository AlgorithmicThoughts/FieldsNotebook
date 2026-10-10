namespace DataContract.Reports;

/// <summary>
/// GET /api/reports/delays — the approved reporting contract, and the whole of it.
/// Both dates are inclusive and compare against the UTC day of Delays.CreatedAt; an omitted date leaves that end open.
/// </summary>
public sealed record DelayReportRequest(int[]? ProjectIds, DateOnly? DateFrom, DateOnly? DateTo);
