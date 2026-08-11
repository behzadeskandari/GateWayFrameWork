using Bank1.Service.Application.Abstractions.External;
using Bank1.Service.Contracts.Accounts;
using Bank1.Service.Domain.Entities;
using FluentValidation;

namespace Bank1.Service.Application.Features.Accounts.GetAccount;

public interface IGetAccountHandler
{
    Task<AccountDetailResponse?> HandleAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class GetAccountHandler : IGetAccountHandler
{
    private readonly IBank1Client _bank1Client;
    private readonly IValidator<GetAccountQuery> _validator;

    public GetAccountHandler(IBank1Client bank1Client, IValidator<GetAccountQuery> validator)
    {
        _bank1Client = bank1Client;
        _validator = validator;
    }

    public async Task<AccountDetailResponse?> HandleAsync(string id, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(new GetAccountQuery(id), cancellationToken);
        var account = await _bank1Client.GetAccountAsync(id, cancellationToken);
        return account is null ? null : MapToDetail(account);
    }

    private static AccountDetailResponse MapToDetail(Account account) =>
        new(
            account.AccountNumber.Value,
            account.HolderName,
            account.Currency,
            account.Status.ToString(),
            account.OpenedAt);
}

public sealed record GetAccountQuery(string Id);
