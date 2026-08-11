using Gateway.Framework.Core.Errors;

namespace Gateway.Framework.Core.Responses;

public sealed record ApiErrorResponse(
    ErrorCode Code,
    string Message,
    string? Detail,
    string? CorrelationId,
    DateTimeOffset Timestamp)
{
    public static ApiErrorResponse From(BankingError error, string? correlationId) =>
        new(error.Code, error.Message, error.Detail, correlationId, DateTimeOffset.UtcNow);
}
