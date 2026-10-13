namespace DataContract.Domain.Delays;

public sealed record ReportPersonRecord(
    int UserId,
    string DisplayName,
    int CrewId,
    string CrewName,
    string RoleName);
