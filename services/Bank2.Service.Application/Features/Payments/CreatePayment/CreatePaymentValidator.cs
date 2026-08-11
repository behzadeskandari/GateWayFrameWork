using Bank2.Service.Contracts.Payments;
using FluentValidation;

namespace Bank2.Service.Application.Features.Payments.CreatePayment;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
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
