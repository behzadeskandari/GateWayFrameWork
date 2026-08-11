using Bank2.Service.Application.Abstractions;
using Bank2.Service.Contracts.Payments;

namespace Bank2.Service.Application.Features.Payments.GetPaymentStatus;

public interface IGetPaymentStatusHandler
{
    Task<PaymentResponse> HandleAsync(string paymentId, CancellationToken cancellationToken = default);
}

public sealed class GetPaymentStatusHandler : IGetPaymentStatusHandler
{
    private readonly IFinancialTransactionService _financialTransactionService;

    public GetPaymentStatusHandler(IFinancialTransactionService financialTransactionService) =>
        _financialTransactionService = financialTransactionService;

    public Task<PaymentResponse> HandleAsync(string paymentId, CancellationToken cancellationToken = default) =>
        _financialTransactionService.GetPaymentStatusAsync(paymentId, cancellationToken);
}
