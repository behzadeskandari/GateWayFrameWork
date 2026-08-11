namespace Gateway.Framework.Core.Errors;

public class DomainException : Exception
{
    public DomainException(ErrorCode code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public ErrorCode Code { get; }
}
