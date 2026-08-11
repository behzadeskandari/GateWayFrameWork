using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly Bank2DbContext _dbContext;

    public IdempotencyStore(Bank2DbContext dbContext) => _dbContext = dbContext;

    public Task<IdempotencyRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.Key == key, cancellationToken);

    public Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.IdempotencyRecords.Add(record);
        return Task.CompletedTask;
    }
}
