using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Features.Payments.GetPayments;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Infrastructure.Persistence;
using Bank2.Service.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task GetPaymentsHandler_ReturnsSeededPayments()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            await DataSeeder.SeedAsync(dbContext);
            var handler = new GetPaymentsHandler(
                new PaymentRepository(dbContext),
                Microsoft.Extensions.Options.Options.Create(new Bank2Options()));

            var response = await handler.HandleAsync("test-correlation");

            Assert.NotEmpty(response.Data);
            Assert.Equal("pay-2001", response.Data[0].Id);
        }
    }

    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_ReturnsSameResult()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(dbContext, auditDbContext);

            var request = new CreatePaymentRequest("acc-1001", "acc-1002", 15m, "USD", "demo");
            var first = await handler.HandleAsync(request, "key-abc", "corr-1");
            var second = await handler.HandleAsync(request, "key-abc", "corr-1");
            Assert.Equal(first.Id, second.Id);
        }
    }
}
