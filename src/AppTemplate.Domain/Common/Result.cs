namespace AppTemplate.Domain.Common;

/// <summary>
/// The outcome of an operation that can fail in an expected way.
/// Either it succeeded, or it carries one or more <see cref="Error"/>s (several for form validation).
/// </summary>
public class Result
{
    private static readonly Result SuccessResult = new([]);

    protected Result(IReadOnlyList<Error> errors) => Errors = errors;

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => SuccessResult;

    public static Result Failure(Error error) => new([error]);

    public static Result Failure(IReadOnlyList<Error> errors)
    {
        ArgumentOutOfRangeException.ThrowIfZero(errors.Count);
        return new(errors);
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    // Lets a method returning Result write `return error;`.
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value when it succeeds.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base([]) => _value = value;

    private Result(IReadOnlyList<Error> errors) : base(errors) { }

    /// <summary>The value. Reading it from a failed result is a bug, so it throws.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result. Check IsSuccess first.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new([error]);

    public static new Result<T> Failure(IReadOnlyList<Error> errors)
    {
        ArgumentOutOfRangeException.ThrowIfZero(errors.Count);
        return new(errors);
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
