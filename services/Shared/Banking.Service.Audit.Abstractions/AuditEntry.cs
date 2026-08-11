namespace Banking.Service.Audit.Abstractions;

public sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required string ServiceName { get; init; }
    public required string EventType { get; init; }
    public required string Operation { get; init; }
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
    public string? RequestId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public string? ActorSubject { get; init; }
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? MetadataJson { get; init; }
}

public interface IAuditWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
