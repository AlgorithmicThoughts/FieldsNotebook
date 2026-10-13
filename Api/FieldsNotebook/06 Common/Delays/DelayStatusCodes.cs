namespace Common.Delays;

/// <summary>DelayStatus.Code values from the approved status contract.</summary>
public static class DelayStatusCodes
{
    public const string Raised = "Raised";
    public const string Acknowledged = "Acknowledged";
    public const string ReadyForHandover = "ReadyForHandover";
    public const string Resolved = "Resolved";
    public const string Reopened = "Reopened";
    public const string Revoked = "Revoked";
}
