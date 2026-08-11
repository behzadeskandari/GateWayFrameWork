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
        DateTimeOffset? createdAt = null)
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

        return new Payment
        {
            Id = id.Trim(),
            FromAccountId = fromAccountId.Trim(),
            ToAccountId = toAccountId.Trim(),
            Amount = amount.Amount,
            Currency = amount.Currency,
            Status = status,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
    }
}
