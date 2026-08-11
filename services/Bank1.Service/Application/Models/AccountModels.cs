namespace Bank1.Service.Application.Models;

public sealed record AccountSummary(string Id, string HolderName, string Currency, decimal Balance);

public sealed record AccountDetail(string Id, string HolderName, string Currency, string Status, DateTimeOffset OpenedAt);

public sealed record AccountBalance(string AccountId, string Currency, decimal AvailableBalance, DateTimeOffset AsOf);
