using Bank1.Service.Domain.Entities;

namespace Bank1.Service.Application.Abstractions.External;

public interface IBank1Client
{
    Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default);
    Task<AccountBalanceSnapshot?> GetBalanceAsync(string id, CancellationToken cancellationToken = default);
}

public sealed record AccountBalanceSnapshot(
    string AccountId,
    string Currency,
    decimal AvailableBalance,
    DateTimeOffset AsOf);
