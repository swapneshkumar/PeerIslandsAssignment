namespace OrderProcessing.Shared.Results;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyCollection<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyCollection<Error> Errors { get; }

    public static Result Success() => new(true, []);
    public static Result Failure(params Error[] errors) => new(false, errors);
}

public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, IReadOnlyCollection<Error> errors) : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, []);
    public static new Result<T> Failure(params Error[] errors) => new(default, false, errors);
}
