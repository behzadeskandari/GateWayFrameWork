namespace Gateway.Framework.Core.Errors;

public sealed class GatewayValidationException : DomainException
{
    public GatewayValidationException(
        string message,
        IReadOnlyDictionary<string, string[]> validationErrors)
        : base(ErrorCode.ValidationFailed, message)
    {
        ValidationErrors = validationErrors;
    }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}
