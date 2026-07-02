namespace OrderProcessing.Shared.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    IReadOnlyCollection<string> Errors,
    string TraceId,
    DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Ok(T? data, string message, string traceId)
        => new(true, message, data, [], traceId, DateTimeOffset.UtcNow);

    public static ApiResponse<T> Fail(string message, IEnumerable<string> errors, string traceId)
        => new(false, message, default, errors.ToArray(), traceId, DateTimeOffset.UtcNow);
}
