using Bank1.Service.Application.Abstractions.External;
using Bank1.Service.Application.Abstractions.Persistence;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Domain.Entities;
using Bank1.Service.Domain.Rules;
using Microsoft.Extensions.Options;

namespace Bank1.Service.Infrastructure.ExternalServices;

public sealed class Bank1BankingProxy : IBank1Client
{
    private readonly IAccountRepository _accountRepository;
    private readonly Bank1ExternalApiClient? _externalApiClient;
    private readonly Bank1ProxyOptions _options;

    public Bank1BankingProxy(
        IAccountRepository accountRepository,
        IOptions<Bank1ProxyOptions> options,
        Bank1ExternalApiClient? externalApiClient = null)
    {
        _accountRepository = accountRepository;
        _options = options.Value;
        _externalApiClient = externalApiClient;
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

    private async Task<Account?> GetAccountFromExternalBankAsync(string id, CancellationToken cancellationToken)
    {
        if (_externalApiClient is null)
        {
            throw new InvalidOperationException("External Bank1 API client is not configured.");
        }

        var external = await _externalApiClient.GetAccountAsync(id, cancellationToken);
        return external is null ? null : Bank1ExternalResponseMapper.MapAccount(external);
    }

    private async Task<AccountBalanceSnapshot?> GetBalanceFromExternalBankAsync(string id, CancellationToken cancellationToken)
    {
        if (_externalApiClient is null)
        {
            throw new InvalidOperationException("External Bank1 API client is not configured.");
        }

        var external = await _externalApiClient.GetBalanceAsync(id, cancellationToken);
        return external is null ? null : Bank1ExternalResponseMapper.MapBalance(external);
    }
}
