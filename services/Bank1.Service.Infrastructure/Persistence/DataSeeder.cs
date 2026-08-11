using Bank1.Service.Domain.Entities;
using Bank1.Service.Domain.Enums;
using Bank1.Service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Bank1.Service.Infrastructure.Persistence;

public static class DataSeeder
{
    private static readonly Guid Account1001Id = Guid.Parse("11111111-1111-1111-1111-000000001001");
    private static readonly Guid Account1002Id = Guid.Parse("11111111-1111-1111-1111-000000001002");

    public static async Task SeedAsync(Bank1DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var accounts = new[]
        {
            new Account(
                Account1001Id,
                new AccountNumber("acc-1001"),
                "Sample Customer One",
                "USD",
                12500.50m,
                AccountStatus.Active,
                DateTimeOffset.UtcNow.AddYears(-2)),
            new Account(
                Account1002Id,
                new AccountNumber("acc-1002"),
                "Sample Customer Two",
                "EUR",
                8420.00m,
                AccountStatus.Active,
                DateTimeOffset.UtcNow.AddYears(-1))
        };

        dbContext.Accounts.AddRange(accounts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
