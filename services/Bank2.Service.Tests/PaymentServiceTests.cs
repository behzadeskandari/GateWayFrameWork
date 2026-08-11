using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Features.Payments.CreatePayment;
using Bank2.Service.Application.Features.Payments.GetPayments;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Infrastructure.ExternalServices;
using Bank2.Service.Infrastructure.Persistence;
using Bank2.Service.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task GetPaymentsHandler_ReturnsSeededPayments()
    {
        await using var dbContext = CreateSeededContext();
        var handler = new GetPaymentsHandler(
            new PaymentRepository(dbContext),
            Options.Create(new Bank2Options()));

        var response = await handler.HandleAsync("test-correlation");

        Assert.NotEmpty(response.Data);
        Assert.Equal("pay-2001", response.Data[0].Id);
    }

    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_ReturnsSameResult()
    {
        await using var dbContext = CreateSeededContext();
        var proxy = new Bank2BankingProxy(
            new PaymentRepository(dbContext),
            new TransferRepository(dbContext),
            Options.Create(new Bank2ProxyOptions()));
        var handler = new CreatePaymentHandler(
            proxy,
            new IdempotencyStore(dbContext),
            new UnitOfWork(dbContext),
            new CreatePaymentValidator());

        var request = new CreatePaymentRequest("acc-1001", "acc-1002", 15m, "USD", "demo");
        var first = await handler.HandleAsync(request, "key-abc");
        var second = await handler.HandleAsync(request, "key-abc");
        Assert.Equal(first.Id, second.Id);
    }

    private static Bank2DbContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<Bank2DbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var dbContext = new Bank2DbContext(options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
        DataSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
        return dbContext;
    }
}
