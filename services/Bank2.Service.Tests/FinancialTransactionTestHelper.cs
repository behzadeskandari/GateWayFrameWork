using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Features.Payments.CreatePayment;
using Bank2.Service.Application.Features.Transfers.CreateTransfer;
using Bank2.Service.Application.Services;
using Bank2.Service.Infrastructure.Persistence;
using Bank2.Service.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Tests;

internal static class FinancialTransactionTestHelper
{
    public static (Bank2DbContext DbContext, Bank2AuditDbContext AuditDbContext) CreateContexts()
    {
        var dbContext = new Bank2DbContext(new DbContextOptionsBuilder<Bank2DbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();

        var auditDbContext = new Bank2AuditDbContext(new DbContextOptionsBuilder<Bank2AuditDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        auditDbContext.Database.OpenConnection();
        auditDbContext.Database.EnsureCreated();

        return (dbContext, auditDbContext);
    }

    public static FinancialTransactionService CreateService(
        Bank2DbContext dbContext,
        Bank2AuditDbContext auditDbContext,
        Bank2ProxyOptions? proxyOptions = null,
        IBank2ExternalGateway? externalGateway = null)
    {
        var options = proxyOptions ?? new Bank2ProxyOptions();
        return new FinancialTransactionService(
            new PaymentRepository(dbContext),
            new TransferRepository(dbContext),
            new IdempotencyStore(dbContext),
            new UnitOfWork(dbContext),
            new AuditService(new AuditWriter(auditDbContext, NullLogger<AuditWriter>.Instance), Options.Create(new Bank2Options())),
            Options.Create(options),
            new CreatePaymentValidator(),
            new CreateTransferValidator(),
            NullLogger<FinancialTransactionService>.Instance,
            externalGateway);
    }

    public static CreatePaymentHandler CreatePaymentHandler(
        Bank2DbContext dbContext,
        Bank2AuditDbContext auditDbContext,
        Bank2ProxyOptions? proxyOptions = null,
        IBank2ExternalGateway? externalGateway = null) =>
        new(CreateService(dbContext, auditDbContext, proxyOptions, externalGateway));

    public static CreateTransferHandler CreateTransferHandler(
        Bank2DbContext dbContext,
        Bank2AuditDbContext auditDbContext,
        Bank2ProxyOptions? proxyOptions = null,
        IBank2ExternalGateway? externalGateway = null) =>
        new(CreateService(dbContext, auditDbContext, proxyOptions, externalGateway));
}
