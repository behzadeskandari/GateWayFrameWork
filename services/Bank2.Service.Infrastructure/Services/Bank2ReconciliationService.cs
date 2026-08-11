using Banking.Service.External.Abstractions;
using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Infrastructure.Services;

public sealed class Bank2ReconciliationService : IReconciliationService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBank2ExternalGateway? _externalGateway;
    private readonly IAuditService _auditService;
    private readonly Bank2ProxyOptions _proxyOptions;
    private readonly Bank2ReconciliationOptions _reconciliationOptions;
    private readonly ILogger<Bank2ReconciliationService> _logger;

    public Bank2ReconciliationService(
        IPaymentRepository paymentRepository,
        ITransferRepository transferRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IOptions<Bank2ProxyOptions> proxyOptions,
        IOptions<Bank2ReconciliationOptions> reconciliationOptions,
        ILogger<Bank2ReconciliationService> logger,
        IBank2ExternalGateway? externalGateway = null)
    {
        _paymentRepository = paymentRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _externalGateway = externalGateway;
        _auditService = auditService;
        _proxyOptions = proxyOptions.Value;
        _reconciliationOptions = reconciliationOptions.Value;
        _logger = logger;
    }

    public async Task ReconcilePaymentsAsync(CancellationToken cancellationToken = default)
    {
        if (!_proxyOptions.Enabled || !_reconciliationOptions.Enabled)
        {
            return;
        }

        var olderThan = DateTimeOffset.UtcNow.AddSeconds(-_reconciliationOptions.MinimumAgeSeconds);
        var candidates = await _paymentRepository.GetUncertainForReconciliationAsync(
            _reconciliationOptions.BatchSize,
            olderThan,
            cancellationToken);

        foreach (var candidate in candidates)
        {
            await TryReconcilePaymentAsync(candidate.Id, cancellationToken);
        }
    }

    public async Task ReconcileTransfersAsync(CancellationToken cancellationToken = default)
    {
        if (!_proxyOptions.Enabled || !_reconciliationOptions.Enabled)
        {
            return;
        }

        var olderThan = DateTimeOffset.UtcNow.AddSeconds(-_reconciliationOptions.MinimumAgeSeconds);
        var candidates = await _transferRepository.GetUncertainForReconciliationAsync(
            _reconciliationOptions.BatchSize,
            olderThan,
            cancellationToken);

        foreach (var candidate in candidates)
        {
            await TryReconcileTransferAsync(candidate.Id, cancellationToken);
        }
    }

    private async Task TryReconcilePaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdForUpdateAsync(paymentId, cancellationToken);
            if (payment is null || TransactionStatusMapper.IsTerminal(payment.Status) || !TransactionStatusMapper.IsUncertain(payment.Status))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(payment.BankReferenceId))
            {
                return;
            }

            var external = await RequireExternalGateway().GetPaymentStatusByBankReferenceAsync(
                payment.BankReferenceId,
                cancellationToken);
            if (external is null)
            {
                return;
            }

            var previousStatus = payment.Status;
            payment.ApplyExternalStatus(external.Status, external.BankReferenceId, external.ErrorCode);
            if (payment.Status == previousStatus)
            {
                return;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.WriteLifecycleEventAsync(
                "PaymentReconciled",
                "Payment",
                payment.Id,
                payment.Status != Domain.Enums.PaymentStatus.Failed,
                payment.CorrelationId,
                payment.ErrorCode,
                cancellationToken: cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogDebug("Skipped payment reconciliation for {PaymentId} due to concurrent update.", paymentId);
        }
        catch (ExternalBankUnavailableException ex)
        {
            _logger.LogWarning(ex, "Payment reconciliation deferred for {PaymentId} because external bank is unavailable.", paymentId);
        }
        catch (ExternalBankTimeoutException ex)
        {
            _logger.LogWarning(ex, "Payment reconciliation deferred for {PaymentId} because external bank timed out.", paymentId);
        }
    }

    private async Task TryReconcileTransferAsync(string transferId, CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await _transferRepository.GetByIdForUpdateAsync(transferId, cancellationToken);
            if (transfer is null || TransactionStatusMapper.IsTerminal(transfer.Status) || !TransactionStatusMapper.IsUncertain(transfer.Status))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(transfer.BankReferenceId))
            {
                return;
            }

            var external = await RequireExternalGateway().GetTransferStatusByBankReferenceAsync(
                transfer.BankReferenceId,
                cancellationToken);
            if (external is null)
            {
                return;
            }

            var previousStatus = transfer.Status;
            transfer.ApplyExternalStatus(external.Status, external.BankReferenceId, external.ErrorCode);
            if (transfer.Status == previousStatus)
            {
                return;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.WriteLifecycleEventAsync(
                "TransferReconciled",
                "Transfer",
                transfer.Id,
                transfer.Status != Domain.Enums.PaymentStatus.Failed,
                transfer.CorrelationId,
                transfer.ErrorCode,
                cancellationToken: cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogDebug("Skipped transfer reconciliation for {TransferId} due to concurrent update.", transferId);
        }
        catch (ExternalBankUnavailableException ex)
        {
            _logger.LogWarning(ex, "Transfer reconciliation deferred for {TransferId} because external bank is unavailable.", transferId);
        }
        catch (ExternalBankTimeoutException ex)
        {
            _logger.LogWarning(ex, "Transfer reconciliation deferred for {TransferId} because external bank timed out.", transferId);
        }
    }

    private IBank2ExternalGateway RequireExternalGateway() =>
        _externalGateway ?? throw new InvalidOperationException("External Bank2 gateway is not configured.");
}
