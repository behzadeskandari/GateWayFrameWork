namespace Banking.Service.External.Abstractions;

public abstract class ExternalBankException : Exception
{
    protected ExternalBankException(string message) : base(message)
    {
    }

    protected ExternalBankException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class ExternalBankAuthenticationException : ExternalBankException
{
    public ExternalBankAuthenticationException(string message) : base(message)
    {
    }
}

public sealed class ExternalBankTimeoutException : ExternalBankException
{
    public ExternalBankTimeoutException(string message) : base(message)
    {
    }

    public ExternalBankTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class ExternalBankUnavailableException : ExternalBankException
{
    public int? StatusCode { get; }

    public ExternalBankUnavailableException(string message, int? statusCode = null) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class ExternalBankResponseException : ExternalBankException
{
    public int StatusCode { get; }

    public string? ErrorCode { get; }

    public ExternalBankResponseException(string message, int statusCode, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
