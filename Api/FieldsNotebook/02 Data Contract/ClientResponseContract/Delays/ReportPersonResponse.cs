namespace DataContract.Delays;

public sealed record ReportPersonResponse(
    int UserId,
    string DisplayName,
    int CrewId,
    string CrewName,
    string RoleName);
