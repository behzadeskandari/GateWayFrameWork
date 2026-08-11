using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Exceptions;
using Bank2.Service.Application.Features.Payments.CreatePayment;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Domain.Enums;
using Bank2.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class IdempotencyTests
{
    [Fact]
    public async Task CreatePayment_SameKeyDifferentPayload_ThrowsConflict()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(dbContext, auditDbContext);

            await handler.HandleAsync(
                new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "first"),
                "shared-key",
                "corr-1");

            await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
                handler.HandleAsync(
                    new CreatePaymentRequest("acc-1001", "acc-1002", 20m, "USD", "second"),
                    "shared-key",
                    "corr-1"));
        }
    }

    [Fact]
    public async Task CreatePayment_SameKeySamePayload_ReturnsSamePaymentId()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(dbContext, auditDbContext);
            var request = new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "same");

            var first = await handler.HandleAsync(request, "stable-key", "corr-1");
            var second = await handler.HandleAsync(request, "stable-key", "corr-1");

            Assert.Equal(first.Id, second.Id);
        }
    }
}
