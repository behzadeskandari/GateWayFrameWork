namespace Gateway.Framework.Core.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? CorrelationId = null,
    DateTimeOffset? Timestamp = null)
{
    public static ApiResponse<T> Ok(T data, string? correlationId = null) =>
        new(true, data, correlationId, DateTimeOffset.UtcNow);
}
