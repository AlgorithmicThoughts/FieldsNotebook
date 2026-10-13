using Common.Results;

namespace Tests;

internal static class ResultAssert
{
    /// <summary>The data of a successful result; a returned error fails the test with its message.</summary>
    public static T Succeeded<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Data;
    }

    public static void Failed<T>(string expectedErrorCode, Result<T> result)
    {
        Assert.True(result.IsFailure, "Expected the operation to be refused.");
        Assert.Equal(expectedErrorCode, result.Error.Code);
    }
}
