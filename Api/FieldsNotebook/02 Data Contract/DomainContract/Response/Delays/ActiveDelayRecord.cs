namespace DataContract.Domain.Delays;

/// <param name="LocationName">Delays.LocationName, or the segment name when none was typed.</param>
/// <param name="SegmentName">Segments.Name of the delay's segment.</param>
/// <param name="CycleCount">All cycles of the delay; more than one means it was reopened.</param>
/// <param name="ExpectedReadyAt">Tentative completion time of the open cycle, in UTC.</param>
public sealed record ActiveDelayRecord(
    int DelayId,
    string Number,
    string Title,
    string Description,
    string StatusCode,
    string TagCode,
    string TagName,
    int OwnerCrewId,
    string OwnerCrewName,
    string ReportType,
    string LocationName,
    string SegmentName,
    int? SiteMapId,
    decimal? PinX,
    decimal? PinY,
    DateTimeOffset CreatedAt,
    int CycleCount,
    bool HasOpenCycle,
    DateTimeOffset? ExpectedReadyAt);
