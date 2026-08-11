using Banking.Service.Audit.Abstractions;
using Bank1.Service.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Logging;

namespace Bank1.Service.Infrastructure.Persistence.Repositories;

public sealed class AuditWriter : IAuditWriter
{
    private readonly Bank1AuditDbContext _auditDbContext;
    private readonly ILogger<AuditWriter> _logger;

    public AuditWriter(Bank1AuditDbContext auditDbContext, ILogger<AuditWriter> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var record = new AuditRecord
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            ServiceName = entry.ServiceName,
            EventType = entry.EventType,
            Operation = entry.Operation,
            CorrelationId = entry.CorrelationId,
            TraceId = entry.TraceId,
            RequestId = entry.RequestId,
            ResourceType = entry.ResourceType,
            ResourceId = entry.ResourceId,
            ActorSubject = entry.ActorSubject,
            Success = entry.Success,
            ErrorCode = entry.ErrorCode,
            MetadataJson = entry.MetadataJson
        };

        _auditDbContext.AuditRecords.Add(record);
        await _auditDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Audit record persisted for operation {Operation} (success={Success})",
            entry.Operation,
            entry.Success);
    }
}
