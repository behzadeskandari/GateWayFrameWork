namespace Bank1.Service.Contracts.Accounts;

public sealed record BalanceResponse(
    string AccountId,
    string Currency,
    decimal AvailableBalance,
    DateTimeOffset AsOf);
