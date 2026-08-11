using Bank1.Service.Application.Abstractions.Persistence;

namespace Bank1.Service.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly Bank1DbContext _dbContext;

    public UnitOfWork(Bank1DbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
