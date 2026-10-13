using System.Diagnostics.CodeAnalysis;
using Common.Errors;

namespace Common.Results;

/// <summary>
/// The outcome of an operation that can fail in an expected way: the data, or the error that says why not.
/// Expected failures — invalid input, no access, not found, a state conflict — are returned, never thrown.
/// Unexpected ones, such as a lost database connection or a missing status row, still throw.
/// </summary>
public sealed class Result<T>
{
    private Result(T? data, RequestError? error)
    {
        Data = data;
        Error = error;
    }

    public T? Data { get; }

    public RequestError? Error { get; }

    [MemberNotNullWhen(true, nameof(Data))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess
    {
        get { return Error is null; }
    }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Data))]
    public bool IsFailure
    {
        get { return Error is not null; }
    }

    public static Result<T> Success(T data)
    {
        return new Result<T>(data, null);
    }

    public static Result<T> Failure(RequestError error)
    {
        return new Result<T>(default, error);
    }

    /// <summary>Lets an operation <c>return data;</c>. C# does not apply this when T is an interface; use <see cref="Success"/> there.</summary>
    public static implicit operator Result<T>(T data)
    {
        return Success(data);
    }

    /// <summary>Lets an operation <c>return error;</c>, including an error passed on from another result.</summary>
    public static implicit operator Result<T>(RequestError error)
    {
        return Failure(error);
    }
}
