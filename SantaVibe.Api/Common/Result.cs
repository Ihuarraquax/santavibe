namespace SantaVibe.Api.Common;

/// <summary>
/// Result type for service operations
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? Message { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private Result(
        bool isSuccess,
        T? value,
        string? error,
        string? message,
        Dictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) =>
        new(true, value, null, null, null);

    public static Result<T> Failure(string error, string message) =>
        new(false, default, error, message, null);

    public static Result<T> ValidationFailure(
        string message,
        Dictionary<string, string[]> errors) =>
        new(false, default, "ValidationError", message, errors);
}
