namespace Bank2.Service.Application.Abstractions;

public interface IAuditService
{
    Task WriteOperationAsync(
        string operation,
        string? resourceType,
        string? resourceId,
        bool success,
        string? correlationId,
        string? errorCode = null,
        CancellationToken cancellationToken = default);

    Task WriteLifecycleEventAsync(
        string operation,
        string resourceType,
        string resourceId,
        bool success,
        string? correlationId,
        string? errorCode = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
}
