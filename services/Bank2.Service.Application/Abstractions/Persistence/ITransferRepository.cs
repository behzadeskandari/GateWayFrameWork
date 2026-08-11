using Bank2.Service.Domain.Entities;

namespace Bank2.Service.Application.Abstractions.Persistence;

public interface ITransferRepository
{
    Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default);
}
