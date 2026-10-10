namespace FieldsNotebook.Controllers
{
    /// <summary>
    /// Local-development identity until Entra ID authentication exists. The header is client-supplied and
    /// spoofable, so it must not reach a deployed environment; Managers still enforce project and role checks.
    /// </summary>
    public static class DemoIdentity
    {
        public const string HeaderName = "X-Demo-User-Id";
    }
}
