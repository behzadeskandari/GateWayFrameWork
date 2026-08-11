using Bank2.Service.Application.Models;

namespace Bank2.Service.Infrastructure.Data;

public interface IPaymentRepository
{
    Task<IReadOnlyList<PaymentSummary>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaymentResult> CreatePaymentAsync(CreatePaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<TransferResult> CreateTransferAsync(CreateTransferRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private static readonly List<PaymentSummary> Payments =
    [
        new("pay-2001", "acc-1001", 250.00m, "USD", "Completed", DateTimeOffset.UtcNow.AddDays(-1))
    ];

    private static readonly Dictionary<string, PaymentResult> IdempotentPayments = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, TransferResult> IdempotentTransfers = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<PaymentSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentSummary>>(Payments.ToList());

    public Task<PaymentResult> CreatePaymentAsync(CreatePaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && IdempotentPayments.TryGetValue(idempotencyKey, out var existing))
        {
            return Task.FromResult(existing);
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        var result = new PaymentResult(
            $"pay-{Guid.NewGuid():N}"[..12],
            "Accepted",
            "Sample payment accepted for demonstration only. No real funds moved.",
            DateTimeOffset.UtcNow);

        Payments.Add(new PaymentSummary(result.Id, request.FromAccountId, request.Amount, request.Currency, result.Status, result.ProcessedAt));

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            IdempotentPayments[idempotencyKey] = result;
        }

        return Task.FromResult(result);
    }

    public Task<TransferResult> CreateTransferAsync(CreateTransferRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && IdempotentTransfers.TryGetValue(idempotencyKey, out var existing))
        {
            return Task.FromResult(existing);
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Transfer amount must be greater than zero.");
        }

        var result = new TransferResult(
            $"trf-{Guid.NewGuid():N}"[..12],
            "Accepted",
            "Sample transfer accepted for demonstration only. No real funds moved.",
            DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            IdempotentTransfers[idempotencyKey] = result;
        }

        return Task.FromResult(result);
    }
}
