using DataContract.Home;

namespace DataContract.Delays;

public sealed record DelayReporterResponse(
    int UserId,
    string DisplayName,
    string CrewName,
    DateTimeOffset ReportedAt);
