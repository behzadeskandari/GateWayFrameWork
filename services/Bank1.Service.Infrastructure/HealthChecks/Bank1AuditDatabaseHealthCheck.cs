using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank1.Service.Infrastructure.HealthChecks;

public sealed class Bank1AuditDatabaseHealthCheck : IHealthCheck
{
    private readonly Persistence.Bank1AuditDbContext _dbContext;

    public Bank1AuditDatabaseHealthCheck(Persistence.Bank1AuditDbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Bank1 audit database is reachable.")
                : HealthCheckResult.Unhealthy("Bank1 audit database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Bank1 audit database health check failed.", ex);
        }
    }
}
