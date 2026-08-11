using Bank1.Service.Application.Features.Accounts.GetAccount;
using FluentValidation;

namespace Bank1.Service.Application.Validators;

public sealed class GetAccountValidator : AbstractValidator<GetAccountQuery>
{
    public GetAccountValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Account id is required.");
    }
}
