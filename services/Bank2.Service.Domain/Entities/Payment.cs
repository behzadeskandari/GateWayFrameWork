using Bank2.Service.Domain.Enums;
using Bank2.Service.Domain.Exceptions;
using Bank2.Service.Domain.ValueObjects;

namespace Bank2.Service.Domain.Entities;

public sealed class Payment
{
    public string Id { get; private set; } = null!;
    public string FromAccountId { get; private set; } = null!;
    public string ToAccountId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? Reference { get; private set; }
    public string? BankReferenceId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [0];

    private Payment()
    {
    }

    public static Payment Create(
        string id,
        string fromAccountId,
        string toAccountId,
        Money amount,
        string? reference,
        PaymentStatus status = PaymentStatus.Accepted,
        DateTimeOffset? createdAt = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        string? bankReferenceId = null)
    {
        ValidateCore(id, fromAccountId, toAccountId, amount);

        return new Payment
        {
            Id = id.Trim(),
            FromAccountId = fromAccountId.Trim(),
            ToAccountId = toAccountId.Trim(),
            Amount = amount.Amount,
            Currency = amount.Currency,
            Status = status,
            Reference = NormalizeOptional(reference),
            IdempotencyKey = NormalizeOptional(idempotencyKey),
            CorrelationId = NormalizeOptional(correlationId),
            BankReferenceId = NormalizeOptional(bankReferenceId),
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
    }

    public static Payment CreateForExternalSubmission(
        string id,
        string fromAccountId,
        string toAccountId,
        Money amount,
        string? reference,
        string idempotencyKey,
        string? correlationId)
    {
        ValidateCore(id, fromAccountId, toAccountId, amount);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainException("Idempotency key is required for external submission.");
        }

        return Create(
            id,
            fromAccountId,
            toAccountId,
            amount,
            reference,
            PaymentStatus.Pending,
            createdAt: DateTimeOffset.UtcNow,
            idempotencyKey: idempotencyKey,
            correlationId: correlationId);
    }

    public void MarkSubmitted()
    {
        if (Status is not PaymentStatus.Pending)
        {
            throw new DomainException($"Payment cannot move to Submitted from {Status}.");
        }

        Status = PaymentStatus.Submitted;
    }

    public void MarkAccepted(string? bankReferenceId)
    {
        BankReferenceId = NormalizeOptional(bankReferenceId) ?? BankReferenceId;
        Status = PaymentStatus.Accepted;
        ErrorCode = null;
    }

    public void MarkCompleted(string? bankReferenceId = null)
    {
        BankReferenceId = NormalizeOptional(bankReferenceId) ?? BankReferenceId;
        Status = PaymentStatus.Completed;
        ErrorCode = null;
    }

    public void MarkFailed(string? errorCode)
    {
        Status = PaymentStatus.Failed;
        ErrorCode = NormalizeOptional(errorCode);
    }

    public void MarkUnknown()
    {
        Status = PaymentStatus.Unknown;
    }

    public void ApplyExternalStatus(PaymentStatus status, string? bankReferenceId, string? errorCode)
    {
        BankReferenceId = NormalizeOptional(bankReferenceId) ?? BankReferenceId;
        Status = status;
        ErrorCode = status == PaymentStatus.Failed ? NormalizeOptional(errorCode) : null;
    }

    private static void ValidateCore(string id, string fromAccountId, string toAccountId, Money amount)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Payment id is required.");
        }

        if (string.IsNullOrWhiteSpace(fromAccountId))
        {
            throw new DomainException("From account id is required.");
        }

        if (string.IsNullOrWhiteSpace(toAccountId))
        {
            throw new DomainException("To account id is required.");
        }

        if (amount.Amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
