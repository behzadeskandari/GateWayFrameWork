using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Application.Features.Payments.GetPayments;

public interface IGetPaymentsHandler
{
    Task<PaymentsListResponse> HandleAsync(string? correlationId, CancellationToken cancellationToken = default);
}

public sealed class GetPaymentsHandler : IGetPaymentsHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly Bank2Options _options;

    public GetPaymentsHandler(IPaymentRepository paymentRepository, IOptions<Bank2Options> options)
    {
        _paymentRepository = paymentRepository;
        _options = options.Value;
    }

    public async Task<PaymentsListResponse> HandleAsync(string? correlationId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetAllAsync(cancellationToken);
        var summaries = payments.Select(MapToSummary).ToList();
        return new PaymentsListResponse(summaries, _options.ServiceName, correlationId);
    }

    private static PaymentSummaryResponse MapToSummary(Payment payment) =>
        new(
            payment.Id,
            payment.FromAccountId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.CreatedAt);
}
