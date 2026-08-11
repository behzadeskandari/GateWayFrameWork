using Bank1.Service.Application.Abstractions.External;
using Bank1.Service.Application.Features.Accounts.GetAccount;
using Bank1.Service.Contracts.Accounts;
using FluentValidation;

namespace Bank1.Service.Application.Features.Accounts.GetBalance;

public interface IGetBalanceHandler
{
    Task<BalanceResponse?> HandleAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class GetBalanceHandler : IGetBalanceHandler
{
    private readonly IBank1Client _bank1Client;
    private readonly IValidator<GetAccountQuery> _validator;

    public GetBalanceHandler(IBank1Client bank1Client, IValidator<GetAccountQuery> validator)
    {
        _bank1Client = bank1Client;
        _validator = validator;
    }

    public async Task<BalanceResponse?> HandleAsync(string id, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(new GetAccountQuery(id), cancellationToken);
        var balance = await _bank1Client.GetBalanceAsync(id, cancellationToken);
        return balance is null
            ? null
            : new BalanceResponse(
                balance.AccountId,
                balance.Currency,
                balance.AvailableBalance,
                balance.AsOf);
    }
}
