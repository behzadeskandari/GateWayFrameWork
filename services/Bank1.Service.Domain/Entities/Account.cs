using Bank1.Service.Domain.Enums;
using Bank1.Service.Domain.Rules;
using Bank1.Service.Domain.ValueObjects;

namespace Bank1.Service.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; private set; }
    public AccountNumber AccountNumber { get; private set; } = null!;
    public string HolderName { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [0];

    private Account()
    {
    }

    public Account(
        Guid id,
        AccountNumber accountNumber,
        string holderName,
        string currency,
        decimal balance,
        AccountStatus status,
        DateTimeOffset openedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Account id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(holderName))
        {
            throw new ArgumentException("Holder name is required.", nameof(holderName));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        AccountRules.EnsureNonNegativeBalance(balance);

        Id = id;
        AccountNumber = accountNumber;
        HolderName = holderName.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        Balance = balance;
        Status = status;
        OpenedAt = openedAt;
    }

    public Money GetBalanceMoney() => new(Balance, Currency);

    public void UpdateBalance(decimal balance)
    {
        AccountRules.EnsureNonNegativeBalance(balance);
        Balance = balance;
    }
}
