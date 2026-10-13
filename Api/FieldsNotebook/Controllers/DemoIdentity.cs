namespace FieldsNotebook.Controllers;

/// <summary>
/// Local-development identity until Entra ID authentication exists. The header is client-supplied and
/// spoofable, so it must not reach a deployed environment; Managers still enforce project and role checks.
/// </summary>
public static class DemoIdentity
{
    public const string HeaderName = "X-Demo-User-Id";

    /// <summary>Reads the demo identity; Managers validate the user ID and project access.</summary>
    public static int GetUserId(HttpContext context) =>
        int.TryParse(context.Request.Headers[HeaderName].ToString(), out var userId) ? userId : 0;
}
