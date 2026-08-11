using Bank2.Service.Domain.Entities;

namespace Bank2.Service.Application.Abstractions.Persistence;

public interface ITransferRepository
{
    Task<IReadOnlyList<Transfer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Transfer?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Transfer?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transfer>> GetUncertainForReconciliationAsync(
        int batchSize,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);

    Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken = default);
}
