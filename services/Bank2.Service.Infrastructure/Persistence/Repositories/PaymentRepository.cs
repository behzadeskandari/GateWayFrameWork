using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private static readonly PaymentStatus[] UncertainStatuses =
    [
        PaymentStatus.Unknown,
        PaymentStatus.Pending,
        PaymentStatus.Submitted
    ];

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

    public Task<Payment?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public Task<Payment?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetUncertainForReconciliationAsync(
        int batchSize,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.Payments.ToListAsync(cancellationToken);

        return candidates
            .Where(payment => payment.CreatedAt <= olderThan)
            .Where(payment => UncertainStatuses.Contains(payment.Status))
            .OrderBy(payment => payment.CreatedAt)
            .Take(batchSize)
            .ToList();
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Update(payment);
        return Task.CompletedTask;
    }
}
