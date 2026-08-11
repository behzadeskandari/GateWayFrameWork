namespace Bank1.Service.Infrastructure.ExternalServices.Models;

public sealed class Bank1ExternalAccountResponse
{
    public string AccountId { get; init; } = null!;
    public string AccountNumber { get; init; } = null!;
    public string HolderName { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public decimal Balance { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset OpenedAt { get; init; }
}

public sealed class Bank1ExternalBalanceResponse
{
    public string AccountId { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public decimal AvailableBalance { get; init; }
    public DateTimeOffset AsOf { get; init; }
}
