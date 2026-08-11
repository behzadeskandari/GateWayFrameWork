using Bank2.Service.Domain.Entities;

namespace Bank2.Service.Application.Abstractions.Persistence;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
