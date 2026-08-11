using Bank1.Service.Application.Abstractions.Persistence;
using Bank1.Service.Domain.Entities;
using Bank1.Service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Bank1.Service.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly Bank1DbContext _dbContext;

    public AccountRepository(Bank1DbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.HolderName)
            .ToListAsync(cancellationToken);

    public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = new AccountNumber(accountNumber.Trim());
        return _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account => account.AccountNumber == normalizedNumber,
                cancellationToken);
    }
}
