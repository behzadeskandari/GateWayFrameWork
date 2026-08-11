using Bank2.Service.Contracts.Transfers;
using FluentValidation;

namespace Bank2.Service.Application.Features.Transfers.CreateTransfer;

public sealed class CreateTransferValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferValidator()
    {
        RuleFor(request => request.FromAccountId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(request => request.ToAccountId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(request => request.Amount)
            .GreaterThan(0);

        RuleFor(request => request.Currency)
            .NotEmpty()
            .Length(3);
    }
}
