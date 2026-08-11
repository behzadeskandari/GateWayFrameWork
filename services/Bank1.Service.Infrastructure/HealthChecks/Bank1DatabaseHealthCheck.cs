using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank1.Service.Infrastructure.HealthChecks;

public sealed class Bank1DatabaseHealthCheck : IHealthCheck
{
    private readonly Persistence.Bank1DbContext _dbContext;

    public Bank1DatabaseHealthCheck(Persistence.Bank1DbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Bank1 database is reachable.")
                : HealthCheckResult.Unhealthy("Bank1 database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Bank1 database health check failed.", ex);
        }
    }
}
