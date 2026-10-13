using Common.Errors;

namespace Tests;

internal static class AuthorizationAssert
{
    /// <summary>The call is refused outright with the expected category instead of returning a result.</summary>
    public static async Task DeniedAsync(string expectedErrorCode, Func<Task> act)
    {
        var denied = await Assert.ThrowsAsync<AuthorizationException>(act);
        Assert.Equal(expectedErrorCode, denied.Error.Code);
    }
}
