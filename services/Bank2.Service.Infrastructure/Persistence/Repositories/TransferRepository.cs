using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private static readonly PaymentStatus[] UncertainStatuses =
    [
        PaymentStatus.Unknown,
        PaymentStatus.Pending,
        PaymentStatus.Submitted
    ];

    private readonly Bank2DbContext _dbContext;

    public TransferRepository(Bank2DbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<Transfer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var transfers = await _dbContext.Transfers
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return transfers
            .OrderByDescending(transfer => transfer.CreatedAt)
            .ToList();
    }

    public Task<Transfer?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        _dbContext.Transfers.AsNoTracking().FirstOrDefaultAsync(transfer => transfer.Id == id, cancellationToken);

    public Task<Transfer?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken = default) =>
        _dbContext.Transfers.FirstOrDefaultAsync(transfer => transfer.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transfer>> GetUncertainForReconciliationAsync(
        int batchSize,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.Transfers.ToListAsync(cancellationToken);

        return candidates
            .Where(transfer => transfer.CreatedAt <= olderThan)
            .Where(transfer => UncertainStatuses.Contains(transfer.Status))
            .OrderBy(transfer => transfer.CreatedAt)
            .Take(batchSize)
            .ToList();
    }

    public Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        _dbContext.Transfers.Add(transfer);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        _dbContext.Transfers.Update(transfer);
        return Task.CompletedTask;
    }
}
