using Banking.Service.Audit.Abstractions;
using Bank1.Service.Application.Abstractions;
using Bank1.Service.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Bank1.Service.Application.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAuditWriter _auditWriter;
    private readonly Bank1Options _options;

    public AuditService(IAuditWriter auditWriter, IOptions<Bank1Options> options)
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
}
