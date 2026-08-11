using Bank1.Service.Application.Configuration;
using Bank1.Service.Infrastructure.ExternalServices;
using Bank1.Service.Infrastructure.Persistence;
using Bank1.Service.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bank1.Service.Tests;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task GetBalance_ForUnknownAccount_ReturnsNull()
    {
        await using var dbContext = CreateSeededContext();
        var repository = new AccountRepository(dbContext);
        var proxy = new Bank1BankingProxy(repository, Options.Create(new Bank1ProxyOptions()));

        var balance = await proxy.GetBalanceAsync("missing");
        Assert.Null(balance);
    }

    [Fact]
    public async Task ListAccounts_ReturnsSampleAccounts()
    {
        await using var dbContext = CreateSeededContext();
        var repository = new AccountRepository(dbContext);
        var accounts = await repository.GetAllAsync();
        Assert.True(accounts.Count >= 2);
    }

    private static Bank1DbContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<Bank1DbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var dbContext = new Bank1DbContext(options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
        DataSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
        return dbContext;
    }
}
