using Bank2.Service.Domain.Entities;

namespace Bank2.Service.Application.Abstractions.Persistence;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IdempotencyAcquireResponse> AcquireAsync(
        string key,
        string operationType,
        string requestFingerprint,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(string key, string responsePayload, CancellationToken cancellationToken = default);
}
