using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly Bank2DbContext _dbContext;

    public IdempotencyStore(Bank2DbContext dbContext) => _dbContext = dbContext;

    public Task<IdempotencyRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(record => record.Key == key, cancellationToken);

    public async Task<IdempotencyAcquireResponse> AcquireAsync(
        string key,
        string operationType,
        string requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            return MapExisting(existing, operationType, requestFingerprint);
        }

        var pendingRecord = IdempotencyRecord.CreatePending(key, operationType, requestFingerprint);
        _dbContext.IdempotencyRecords.Add(pendingRecord);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new IdempotencyAcquireResponse { Result = IdempotencyAcquireResult.Acquired };
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(pendingRecord).State = EntityState.Detached;
            existing = await GetByKeyAsync(key, cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return MapExisting(existing, operationType, requestFingerprint);
        }
    }

    public async Task CompleteAsync(string key, string responsePayload, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords
            .FirstAsync(item => item.Key == key, cancellationToken);
        record.Complete(responsePayload);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IdempotencyAcquireResponse MapExisting(
        IdempotencyRecord existing,
        string operationType,
        string requestFingerprint)
    {
        if (!string.Equals(existing.OperationType, operationType, StringComparison.Ordinal) ||
            !string.Equals(existing.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            return new IdempotencyAcquireResponse { Result = IdempotencyAcquireResult.Conflict };
        }

        if (existing.Status == IdempotencyStatus.Pending)
        {
            return new IdempotencyAcquireResponse { Result = IdempotencyAcquireResult.InProgress };
        }

        return new IdempotencyAcquireResponse
        {
            Result = IdempotencyAcquireResult.Completed,
            ResponsePayload = existing.ResponsePayload
        };
    }
}
