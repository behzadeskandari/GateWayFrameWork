using Bank2.Service.Application.Abstractions.Persistence;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly Bank2DbContext _dbContext;

    public UnitOfWork(Bank2DbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
