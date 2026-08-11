using Bank1.Service.Application.Abstractions.External;
using Bank1.Service.Application.Abstractions.Persistence;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Domain.Rules;
using Bank1.Service.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Bank1.Service.Infrastructure.ExternalServices;

public sealed class Bank1BankingProxy : IBank1Client
{
    private readonly IAccountRepository _accountRepository;
    private readonly Bank1ProxyOptions _options;
    private readonly HttpClient? _httpClient;

    public Bank1BankingProxy(
        IAccountRepository accountRepository,
        IOptions<Bank1ProxyOptions> options,
        HttpClient? httpClient = null)
    {
        _accountRepository = accountRepository;
        _options = options.Value;
        _httpClient = httpClient;
    }

    public Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return _accountRepository.GetByAccountNumberAsync(id, cancellationToken);
        }

        return GetAccountFromExternalBankAsync(id, cancellationToken);
    }

    public async Task<AccountBalanceSnapshot?> GetBalanceAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            var account = await _accountRepository.GetByAccountNumberAsync(id, cancellationToken);
            if (account is null)
            {
                return null;
            }

            AccountRules.EnsureActiveForBalanceInquiry(account.Status);
            return new AccountBalanceSnapshot(
                account.AccountNumber.Value,
                account.Currency,
                account.Balance,
                DateTimeOffset.UtcNow);
        }

        return await GetBalanceFromExternalBankAsync(id, cancellationToken);
    }

    private Task<Account?> GetAccountFromExternalBankAsync(string id, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
        {
            throw new InvalidOperationException("REQUIRES REAL BANK INTEGRATION");
        }

        _ = id;
        _ = cancellationToken;
        throw new NotImplementedException("REQUIRES REAL BANK INTEGRATION");
    }

    private Task<AccountBalanceSnapshot?> GetBalanceFromExternalBankAsync(string id, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
        {
            throw new InvalidOperationException("REQUIRES REAL BANK INTEGRATION");
        }

        _ = id;
        _ = cancellationToken;
        throw new NotImplementedException("REQUIRES REAL BANK INTEGRATION");
    }
}
