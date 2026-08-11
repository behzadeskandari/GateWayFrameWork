namespace Bank2.Service.Application.Exceptions;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message) : base(message)
    {
    }
}

public sealed class IdempotencyInProgressException : Exception
{
    public IdempotencyInProgressException(string message) : base(message)
    {
    }
}
