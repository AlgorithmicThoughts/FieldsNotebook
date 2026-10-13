namespace DataContract.Domain.Delays;

public sealed record TrackingDelayRecord(
    ActiveDelayRecord Summary,
    int ReporterId,
    string ReporterName,
    string ReporterCrewName);
