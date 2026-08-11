using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Exceptions;
using Bank2.Service.Application.Features.Payments.GetPaymentStatus;
using Bank2.Service.Application.Services;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Enums;
using Bank2.Service.Infrastructure.ExternalServices;
using Bank2.Service.Infrastructure.ExternalServices.Models;
using Bank2.Service.Infrastructure.Persistence;
using Banking.Service.External.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class FinancialTransactionSafetyTests
{
    [Fact]
    public async Task ExternalSubmitSuccess_PersistsBankReferenceIdAndAcceptedStatus()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var gateway = new FakeExternalGateway(
                new ExternalSubmissionResult(
                    PaymentStatus.Accepted,
                    "accepted",
                    DateTimeOffset.UtcNow,
                    "BANK-123"));

            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(
                dbContext,
                auditDbContext,
                new Bank2ProxyOptions { Enabled = true },
                gateway);

            var response = await handler.HandleAsync(
                new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "ref"),
                "idem-1",
                "corr-1");

            var payment = await dbContext.Payments.SingleAsync();
            Assert.Equal("BANK-123", payment.BankReferenceId);
            Assert.Equal(PaymentStatus.Accepted, payment.Status);
            Assert.Equal("BANK-123", response.BankReferenceId);
            Assert.False(response.RequiresStatusInquiry);
        }
    }

    [Fact]
    public async Task ExternalSubmitDefinitiveFailure_MarksPaymentFailed()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var gateway = new FakeExternalGateway(
                submitPaymentException: new ExternalBankResponseException("Rejected", 400, "BUSINESS_RULE"));

            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(
                dbContext,
                auditDbContext,
                new Bank2ProxyOptions { Enabled = true },
                gateway);

            var response = await handler.HandleAsync(
                new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "ref"),
                "idem-fail",
                "corr-1");

            var payment = await dbContext.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.Failed, payment.Status);
            Assert.Equal("Failed", response.Status);
        }
    }

    [Fact]
    public async Task ExternalSubmitTimeout_MarksPaymentUnknownAndCompletesIdempotency()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var gateway = new FakeExternalGateway(
                submitPaymentException: new ExternalBankTimeoutException("timeout"));

            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(
                dbContext,
                auditDbContext,
                new Bank2ProxyOptions { Enabled = true },
                gateway);

            var response = await handler.HandleAsync(
                new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "ref"),
                "idem-timeout",
                "corr-1");

            Assert.Equal("Unknown", response.Status);
            Assert.True(response.RequiresStatusInquiry);

            var payment = await dbContext.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.Unknown, payment.Status);

            var record = await dbContext.IdempotencyRecords.SingleAsync();
            Assert.Equal("Completed", record.Status.ToString());

            var retry = await handler.HandleAsync(
                new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "ref"),
                "idem-timeout",
                "corr-1");
            Assert.Equal(response.Id, retry.Id);
            Assert.Equal(1, await dbContext.Payments.CountAsync());
        }
    }

    [Fact]
    public async Task ExternalSubmitRequiresIdempotencyKey()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var handler = FinancialTransactionTestHelper.CreatePaymentHandler(
                dbContext,
                auditDbContext,
                new Bank2ProxyOptions { Enabled = true },
                new FakeExternalGateway(new ExternalSubmissionResult(PaymentStatus.Accepted, "ok", DateTimeOffset.UtcNow, "BANK-1")));

            await Assert.ThrowsAsync<IdempotencyKeyRequiredException>(() =>
                handler.HandleAsync(
                    new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "ref"),
                    null,
                    "corr-1"));
        }
    }

    [Fact]
    public async Task GetPaymentStatus_UsesInternalIdAndBankReferenceForRefresh()
    {
        var (dbContext, auditDbContext) = FinancialTransactionTestHelper.CreateContexts();
        await using (dbContext)
        await using (auditDbContext)
        {
            var gateway = new FakeExternalGateway(
                new ExternalSubmissionResult(PaymentStatus.Unknown, "unknown", DateTimeOffset.UtcNow, "BANK-999"),
                new ExternalSubmissionResult(PaymentStatus.Completed, "done", DateTimeOffset.UtcNow, "BANK-999"));

            var createHandler = FinancialTransactionTestHelper.CreatePaymentHandler(
                dbContext,
                auditDbContext,
                new Bank2ProxyOptions { Enabled = true },
                gateway);

            var created = await createHandler.HandleAsync(
                new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "ref"),
                "idem-status",
                "corr-1");

            gateway.UseStatusResultForRefresh();

            var statusHandler = new GetPaymentStatusHandler(
                FinancialTransactionTestHelper.CreateService(
                    dbContext,
                    auditDbContext,
                    new Bank2ProxyOptions { Enabled = true },
                    gateway));

            var status = await statusHandler.HandleAsync(created.Id);
            Assert.Equal("Completed", status.Status);
            Assert.Equal("BANK-999", status.BankReferenceId);
        }
    }

    [Fact]
    public async Task Bank2ExternalGateway_MapsBankReferenceId()
    {
        var mapped = Bank2ExternalGateway.MapPayment(new Bank2ExternalPaymentResponse
        {
            Id = "ext-1",
            Status = "Accepted",
            Message = "ok",
            ProcessedAt = DateTimeOffset.UtcNow,
            BankReferenceId = "BANK-XYZ"
        });

        Assert.Equal("BANK-XYZ", mapped.BankReferenceId);
        Assert.Equal(PaymentStatus.Accepted, mapped.Status);
    }

    private sealed class FakeExternalGateway : IBank2ExternalGateway
    {
        private readonly ExternalSubmissionResult? _submitPaymentResult;
        private readonly ExternalSubmissionResult? _submitTransferResult;
        private readonly ExternalSubmissionResult? _statusResult;
        private readonly Exception? _submitPaymentException;
        private readonly Exception? _submitTransferException;
        private bool _useStatusResult;

        public FakeExternalGateway(
            ExternalSubmissionResult? submitPaymentResult = null,
            ExternalSubmissionResult? statusResult = null,
            Exception? submitPaymentException = null)
        {
            _submitPaymentResult = submitPaymentResult;
            _statusResult = statusResult;
            _submitPaymentException = submitPaymentException;
        }

        public void UseStatusResultForRefresh() => _useStatusResult = true;

        public Task<ExternalSubmissionResult> SubmitPaymentAsync(
            CreatePaymentRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            if (_submitPaymentException is not null)
            {
                throw _submitPaymentException;
            }

            return Task.FromResult(_submitPaymentResult!);
        }

        public Task<ExternalSubmissionResult> SubmitTransferAsync(
            CreateTransferRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            if (_submitTransferException is not null)
            {
                throw _submitTransferException;
            }

            return Task.FromResult(_submitTransferResult ?? _submitPaymentResult!);
        }

        public Task<ExternalSubmissionResult?> GetPaymentStatusByBankReferenceAsync(
            string bankReferenceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExternalSubmissionResult?>(_useStatusResult ? _statusResult : null);

        public Task<ExternalSubmissionResult?> GetTransferStatusByBankReferenceAsync(
            string bankReferenceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExternalSubmissionResult?>(_useStatusResult ? _statusResult : null);
    }
}
