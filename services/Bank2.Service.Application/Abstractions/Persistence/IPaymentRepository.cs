using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;

namespace Bank2.Service.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetUncertainForReconciliationAsync(
        int batchSize,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}
