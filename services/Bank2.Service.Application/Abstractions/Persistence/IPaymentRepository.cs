using Bank2.Service.Domain.Entities;

namespace Bank2.Service.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
