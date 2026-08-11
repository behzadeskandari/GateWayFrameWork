using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Services;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Enums;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.ValueObjects;
using Bank2.Service.Infrastructure.Persistence;
using Bank2.Service.Infrastructure.Persistence.Repositories;
using Bank2.Service.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class ReconciliationTests
{
    [Fact]
    public async Task ReconcilePayment_UnknownToCompleted()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var payment = Payment.CreateForExternalSubmission(
                "pay-test001",
                "acc-1001",
                "acc-1002",
                new Money(10m, "USD"),
                "ref",
                "idem-1",
                "corr-1");
            payment.MarkUnknown();
            payment.ApplyExternalStatus(PaymentStatus.Unknown, "BANK-REC-1", null);
            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            var gateway = new FakeReconciliationGateway(
                new ExternalSubmissionResult(PaymentStatus.Completed, "done", DateTimeOffset.UtcNow, "BANK-REC-1"));

            var service = new Bank2ReconciliationService(
                new PaymentRepository(dbContext),
                new TransferRepository(dbContext),
                new UnitOfWork(dbContext),
                new AuditService(new AuditWriter(auditDbContext, NullLogger<AuditWriter>.Instance), Options.Create(new Bank2Options())),
                Options.Create(new Bank2ProxyOptions { Enabled = true }),
                Options.Create(new Bank2ReconciliationOptions { Enabled = true, MinimumAgeSeconds = 0 }),
                NullLogger<Bank2ReconciliationService>.Instance,
                gateway);

            await service.ReconcilePaymentsAsync();

            var updated = await dbContext.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.Completed, updated.Status);
        }
    }

    [Fact]
    public async Task ReconcilePayment_DoesNotResubmitFinancialOperation()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var gateway = new FakeReconciliationGateway(
                new ExternalSubmissionResult(PaymentStatus.Completed, "done", DateTimeOffset.UtcNow, "BANK-REC-2"));

            var payment = Payment.CreateForExternalSubmission(
                "pay-test002",
                "acc-1001",
                "acc-1002",
                new Money(10m, "USD"),
                "ref",
                "idem-2",
                "corr-2");
            payment.MarkUnknown();
            payment.ApplyExternalStatus(PaymentStatus.Unknown, "BANK-REC-2", null);
            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            var service = new Bank2ReconciliationService(
                new PaymentRepository(dbContext),
                new TransferRepository(dbContext),
                new UnitOfWork(dbContext),
                new AuditService(new AuditWriter(auditDbContext, NullLogger<AuditWriter>.Instance), Options.Create(new Bank2Options())),
                Options.Create(new Bank2ProxyOptions { Enabled = true }),
                Options.Create(new Bank2ReconciliationOptions { Enabled = true, MinimumAgeSeconds = 0 }),
                NullLogger<Bank2ReconciliationService>.Instance,
                gateway);

            await service.ReconcilePaymentsAsync();
            Assert.Equal(0, gateway.SubmitPaymentCalls);
        }
    }

    private sealed class FakeReconciliationGateway : IBank2ExternalGateway
    {
        private readonly ExternalSubmissionResult _statusResult;
        public int SubmitPaymentCalls { get; private set; }

        public FakeReconciliationGateway(ExternalSubmissionResult statusResult) =>
            _statusResult = statusResult;

        public Task<ExternalSubmissionResult> SubmitPaymentAsync(
            CreatePaymentRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            SubmitPaymentCalls++;
            throw new InvalidOperationException("Reconciliation must not submit payments.");
        }

        public Task<ExternalSubmissionResult> SubmitTransferAsync(
            CreateTransferRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Reconciliation must not submit transfers.");

        public Task<ExternalSubmissionResult?> GetPaymentStatusByBankReferenceAsync(
            string bankReferenceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExternalSubmissionResult?>(_statusResult);

        public Task<ExternalSubmissionResult?> GetTransferStatusByBankReferenceAsync(
            string bankReferenceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExternalSubmissionResult?>(null);
    }
}
