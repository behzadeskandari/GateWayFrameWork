using Gateway.Framework.Core.Errors;

namespace Gateway.Framework.Core.Responses;

public sealed record ApiValidationErrorResponse(
    ErrorCode Code,
    string Message,
    IReadOnlyDictionary<string, string[]> Errors,
    string? CorrelationId,
    DateTimeOffset Timestamp)
{
    public static ApiValidationErrorResponse From(
        GatewayValidationException exception,
        string? correlationId) =>
        new(
            exception.Code,
            exception.Message,
            exception.ValidationErrors,
            correlationId,
            DateTimeOffset.UtcNow);
}
