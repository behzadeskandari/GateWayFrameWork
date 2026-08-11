namespace Gateway.Framework.Core.Errors;

public sealed record BankingError(
    ErrorCode Code,
    string Message,
    string? Detail = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
