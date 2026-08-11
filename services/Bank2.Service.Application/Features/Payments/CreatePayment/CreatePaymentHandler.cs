using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Features.Payments.CreatePayment;
using Bank2.Service.Contracts.Payments;
using FluentValidation;

namespace Bank2.Service.Application.Features.Payments.CreatePayment;

public interface ICreatePaymentHandler
{
    Task<PaymentResponse> HandleAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken = default);
}

public sealed class CreatePaymentHandler : ICreatePaymentHandler
{
    private readonly IFinancialTransactionService _financialTransactionService;

    public CreatePaymentHandler(IFinancialTransactionService financialTransactionService) =>
        _financialTransactionService = financialTransactionService;

    public Task<PaymentResponse> HandleAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken = default) =>
        _financialTransactionService.SubmitPaymentAsync(request, idempotencyKey, correlationId, cancellationToken);
}
