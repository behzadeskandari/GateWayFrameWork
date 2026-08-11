namespace Gateway.Framework.Logging.Audit;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string resource,
        string outcome,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
