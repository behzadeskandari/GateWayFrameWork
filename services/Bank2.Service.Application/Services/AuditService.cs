using System.Text.Json;
using Banking.Service.Audit.Abstractions;
using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Application.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAuditWriter _auditWriter;
    private readonly Bank2Options _options;

    public AuditService(IAuditWriter auditWriter, IOptions<Bank2Options> options)
    {
        _auditWriter = auditWriter;
        _options = options.Value;
    }

    public Task WriteOperationAsync(
        string operation,
        string? resourceType,
        string? resourceId,
        bool success,
        string? correlationId,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry
        {
            ServiceName = _options.ServiceName,
            EventType = "ApiOperation",
            Operation = operation,
            CorrelationId = correlationId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Success = success,
            ErrorCode = errorCode
        };

        return _auditWriter.WriteAsync(entry, cancellationToken);
    }

    public Task WriteLifecycleEventAsync(
        string operation,
        string resourceType,
        string resourceId,
        bool success,
        string? correlationId,
        string? errorCode = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry
        {
            ServiceName = _options.ServiceName,
            EventType = "FinancialLifecycle",
            Operation = operation,
            CorrelationId = correlationId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Success = success,
            ErrorCode = errorCode,
            MetadataJson = metadataJson
        };

        return _auditWriter.WriteAsync(entry, cancellationToken);
    }
}
