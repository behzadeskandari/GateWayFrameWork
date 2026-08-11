using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank2.Service.Infrastructure.HealthChecks;

public sealed class Bank2AuditDatabaseHealthCheck : IHealthCheck
{
    private readonly Persistence.Bank2AuditDbContext _dbContext;

    public Bank2AuditDatabaseHealthCheck(Persistence.Bank2AuditDbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Bank2 audit database is reachable.")
                : HealthCheckResult.Unhealthy("Bank2 audit database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Bank2 audit database health check failed.", ex);
        }
    }
}
