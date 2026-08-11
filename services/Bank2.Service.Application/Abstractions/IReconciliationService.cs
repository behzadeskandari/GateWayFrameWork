namespace Bank2.Service.Application.Abstractions;

public interface IReconciliationService
{
    Task ReconcilePaymentsAsync(CancellationToken cancellationToken = default);

    Task ReconcileTransfersAsync(CancellationToken cancellationToken = default);
}
