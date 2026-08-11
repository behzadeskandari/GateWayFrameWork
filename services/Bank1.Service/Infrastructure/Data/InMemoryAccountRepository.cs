using Bank1.Service.Application.Models;

namespace Bank1.Service.Infrastructure.Data;

public interface IAccountRepository
{
    Task<IReadOnlyList<AccountSummary>> ListAsync(CancellationToken cancellationToken = default);
    Task<AccountDetail?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<AccountBalance?> GetBalanceAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private static readonly Dictionary<string, (AccountDetail Detail, decimal Balance)> Accounts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acc-1001"] = (new AccountDetail("acc-1001", "Sample Customer One", "USD", "Active", DateTimeOffset.UtcNow.AddYears(-2)), 12500.50m),
        ["acc-1002"] = (new AccountDetail("acc-1002", "Sample Customer Two", "EUR", "Active", DateTimeOffset.UtcNow.AddYears(-1)), 8420.00m)
    };

    public Task<IReadOnlyList<AccountSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var summaries = Accounts.Values
            .Select(x => new AccountSummary(x.Detail.Id, x.Detail.HolderName, x.Detail.Currency, x.Balance))
            .ToList();
        return Task.FromResult<IReadOnlyList<AccountSummary>>(summaries);
    }

    public Task<AccountDetail?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Accounts.TryGetValue(id, out var account) ? account.Detail : null);

    public Task<AccountBalance?> GetBalanceAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Accounts.TryGetValue(id, out var account))
        {
            return Task.FromResult<AccountBalance?>(null);
        }

        return Task.FromResult<AccountBalance?>(new AccountBalance(
            account.Detail.Id,
            account.Detail.Currency,
            account.Balance,
            DateTimeOffset.UtcNow));
    }
}
