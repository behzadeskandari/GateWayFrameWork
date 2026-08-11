using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly Bank2DbContext _dbContext;

    public PaymentRepository(Bank2DbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var payments = await _dbContext.Payments
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return payments
            .OrderByDescending(payment => payment.CreatedAt)
            .ToList();
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Add(payment);
        return Task.CompletedTask;
    }
}
