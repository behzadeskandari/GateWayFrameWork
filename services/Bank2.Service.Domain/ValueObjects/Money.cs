using Bank2.Service.Domain.Exceptions;

namespace Bank2.Service.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public bool Equals(Money? other) =>
        other is not null &&
        Amount == other.Amount &&
        Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency.ToUpperInvariant());

    public override string ToString() => $"{Amount:F2} {Currency}";
}
