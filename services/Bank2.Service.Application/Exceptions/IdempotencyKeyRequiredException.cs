namespace Bank2.Service.Application.Exceptions;

public sealed class IdempotencyKeyRequiredException : Exception
{
    public IdempotencyKeyRequiredException()
        : base("Idempotency-Key header is required for financial operations when external proxy is enabled.")
    {
    }
}
