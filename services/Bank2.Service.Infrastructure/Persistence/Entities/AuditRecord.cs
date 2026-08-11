namespace Bank2.Service.Infrastructure.Persistence.Entities;

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string ServiceName { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Operation { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? RequestId { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? ActorSubject { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? MetadataJson { get; set; }
}
