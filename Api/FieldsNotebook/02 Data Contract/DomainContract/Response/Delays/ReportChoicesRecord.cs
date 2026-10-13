namespace DataContract.Domain.Delays;

/// <summary>The project's active crews and the active delay reasons, without the rest of the report form.</summary>
public sealed record ReportChoicesRecord(
    IReadOnlyList<ReportCrewRecord> Crews,
    IReadOnlyList<ReportTagRecord> Tags);
