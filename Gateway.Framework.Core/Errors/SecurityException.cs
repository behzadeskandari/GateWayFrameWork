namespace Gateway.Framework.Core.Errors;

public sealed class SecurityException : DomainException
{
    public SecurityException(ErrorCode code, string message)
        : base(code, message)
    {
    }
}
