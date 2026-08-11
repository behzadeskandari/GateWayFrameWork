using Bank1.Service.Application.Models;

namespace Bank1.Service.Application.Services;

public interface IAccountService
{
    Task<IReadOnlyList<AccountSummary>> ListAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountDetail?> GetAccountAsync(string id, CancellationToken cancellationToken = default);
    Task<AccountBalance?> GetBalanceAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class AccountService : IAccountService
{
    private readonly Infrastructure.Data.IAccountRepository _repository;

    public AccountService(Infrastructure.Data.IAccountRepository repository) => _repository = repository;

    public Task<IReadOnlyList<AccountSummary>> ListAccountsAsync(CancellationToken cancellationToken = default) =>
        _repository.ListAsync(cancellationToken);

    public Task<AccountDetail?> GetAccountAsync(string id, CancellationToken cancellationToken = default) =>
        _repository.GetAsync(id, cancellationToken);

    public Task<AccountBalance?> GetBalanceAsync(string id, CancellationToken cancellationToken = default) =>
        _repository.GetBalanceAsync(id, cancellationToken);
}
