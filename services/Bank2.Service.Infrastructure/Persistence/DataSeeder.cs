using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Bank2.Service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(Bank2DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Payments.AnyAsync(cancellationToken))
        {
            return;
        }

        var samplePayment = Payment.Create(
            "pay-2001",
            "acc-1001",
            "acc-1002",
            new Money(250.00m, "USD"),
            "sample-seed",
            PaymentStatus.Completed,
            DateTimeOffset.UtcNow.AddDays(-1));

        dbContext.Payments.Add(samplePayment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
