using Bank1.Service.Application.Abstractions.Persistence;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Contracts.Accounts;
using Bank1.Service.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Bank1.Service.Application.Features.Accounts.GetAccounts;

public interface IGetAccountsHandler
{
    Task<AccountsListResponse> HandleAsync(string? correlationId, CancellationToken cancellationToken = default);
}

public sealed class GetAccountsHandler : IGetAccountsHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly Bank1Options _options;

    public GetAccountsHandler(IAccountRepository accountRepository, IOptions<Bank1Options> options)
    {
        _accountRepository = accountRepository;
        _options = options.Value;
    }

    public async Task<AccountsListResponse> HandleAsync(string? correlationId, CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var summaries = accounts.Select(MapToSummary).ToList();
        return new AccountsListResponse(summaries, _options.ServiceName, correlationId);
    }

    private static AccountSummaryResponse MapToSummary(Account account) =>
        new(
            account.AccountNumber.Value,
            account.HolderName,
            account.Currency,
            account.Balance);
}
