using Bank2.Service.Application.Models;
using Bank2.Service.Infrastructure.Data;

namespace Bank2.Service.Application.Services;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentSummary>> ListPaymentsAsync(CancellationToken cancellationToken = default);
    Task<PaymentResult> CreatePaymentAsync(CreatePaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<TransferResult> CreateTransferAsync(CreateTransferRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;

    public PaymentService(IPaymentRepository repository) => _repository = repository;

    public Task<IReadOnlyList<PaymentSummary>> ListPaymentsAsync(CancellationToken cancellationToken = default) =>
        _repository.ListAsync(cancellationToken);

    public Task<PaymentResult> CreatePaymentAsync(CreatePaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default) =>
        _repository.CreatePaymentAsync(request, idempotencyKey, cancellationToken);

    public Task<TransferResult> CreateTransferAsync(CreateTransferRequest request, string? idempotencyKey, CancellationToken cancellationToken = default) =>
        _repository.CreateTransferAsync(request, idempotencyKey, cancellationToken);
}
