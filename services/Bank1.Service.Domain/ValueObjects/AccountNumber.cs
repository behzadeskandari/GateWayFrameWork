using Bank1.Service.Domain.Exceptions;

namespace Bank1.Service.Domain.ValueObjects;

public sealed class AccountNumber : IEquatable<AccountNumber>
{
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Account number cannot be empty.");
        }

        Value = value.Trim();
    }

    public bool Equals(AccountNumber? other) =>
        other is not null &&
        Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
