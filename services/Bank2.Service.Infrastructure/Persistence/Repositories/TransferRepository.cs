using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private readonly Bank2DbContext _dbContext;

    public TransferRepository(Bank2DbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        _dbContext.Transfers.Add(transfer);
        return Task.CompletedTask;
    }
}
