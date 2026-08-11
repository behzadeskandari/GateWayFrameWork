using Bank2.Service.Domain.Exceptions;

namespace Bank2.Service.Domain.Entities;

public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = null!;
    public string OperationType { get; private set; } = null!;
    public string ResponsePayload { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
    }

    public static IdempotencyRecord Create(string key, string operationType, string responsePayload)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Idempotency key is required.");
        }

        if (string.IsNullOrWhiteSpace(operationType))
        {
            throw new DomainException("Operation type is required.");
        }

        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            throw new DomainException("Response payload is required.");
        }

        return new IdempotencyRecord
        {
            Key = key.Trim(),
            OperationType = operationType.Trim(),
            ResponsePayload = responsePayload,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
