using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank2.Service.Infrastructure.HealthChecks;

public sealed class Bank2DatabaseHealthCheck : IHealthCheck
{
    private readonly Persistence.Bank2DbContext _dbContext;

    public Bank2DatabaseHealthCheck(Persistence.Bank2DbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Bank2 database is reachable.")
                : HealthCheckResult.Unhealthy("Bank2 database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Bank2 database health check failed.", ex);
        }
    }
}
