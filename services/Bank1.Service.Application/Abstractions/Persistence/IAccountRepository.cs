using Bank1.Service.Domain.Entities;

namespace Bank1.Service.Application.Abstractions.Persistence;

public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
}
