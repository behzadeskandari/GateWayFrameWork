using Gateway.Framework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Gateway.Framework.Logging.Audit;

public sealed class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IClock _clock;

    public AuditLogger(
        ILogger<AuditLogger> logger,
        ICorrelationIdAccessor correlationIdAccessor,
        IClock clock)
    {
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
        _clock = clock;
    }

    public Task LogAsync(
        string action,
        string resource,
        string outcome,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AUDIT action={Action} resource={Resource} outcome={Outcome} correlationId={CorrelationId} timestamp={Timestamp} metadata={Metadata}",
            action,
            resource,
            outcome,
            _correlationIdAccessor.CorrelationId,
            _clock.UtcNow,
            metadata);

        return Task.CompletedTask;
    }
}
