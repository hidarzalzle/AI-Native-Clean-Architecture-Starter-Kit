namespace SharedKernel.Common;

public record Error(string Code, string Message);

public class Result
{
    public bool IsSuccess { get; init; }
    public Error? Error { get; init; }
    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string code, string message) => new() { IsSuccess = false, Error = new Error(code, message) };
}

public class Result<T> : Result
{
    public T? Value { get; init; }
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public new static Result<T> Failure(string code, string message) => new() { IsSuccess = false, Error = new Error(code, message) };
}
