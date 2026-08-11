using Bank2.Service.Domain.Enums;
using Bank2.Service.Domain.Exceptions;

namespace Bank2.Service.Domain.Entities;

public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = null!;
    public string OperationType { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public IdempotencyStatus Status { get; private set; }
    public string? ResponsePayload { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
    }

    public static IdempotencyRecord CreatePending(string key, string operationType, string requestFingerprint)
    {
        ValidateKeyOperationAndFingerprint(key, operationType, requestFingerprint);

        return new IdempotencyRecord
        {
            Key = key.Trim(),
            OperationType = operationType.Trim(),
            RequestFingerprint = requestFingerprint.Trim(),
            Status = IdempotencyStatus.Pending,
            ResponsePayload = null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static IdempotencyRecord CreateCompleted(
        string key,
        string operationType,
        string requestFingerprint,
        string responsePayload)
    {
        ValidateKeyOperationAndFingerprint(key, operationType, requestFingerprint);

        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            throw new DomainException("Response payload is required.");
        }

        return new IdempotencyRecord
        {
            Key = key.Trim(),
            OperationType = operationType.Trim(),
            RequestFingerprint = requestFingerprint.Trim(),
            Status = IdempotencyStatus.Completed,
            ResponsePayload = responsePayload,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Complete(string responsePayload)
    {
        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            throw new DomainException("Response payload is required.");
        }

        ResponsePayload = responsePayload;
        Status = IdempotencyStatus.Completed;
    }

    private static void ValidateKeyOperationAndFingerprint(string key, string operationType, string requestFingerprint)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Idempotency key is required.");
        }

        if (string.IsNullOrWhiteSpace(operationType))
        {
            throw new DomainException("Operation type is required.");
        }

        if (string.IsNullOrWhiteSpace(requestFingerprint))
        {
            throw new DomainException("Request fingerprint is required.");
        }
    }
}
