using Bank1.Service.Infrastructure.ExternalServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank1.Service.Infrastructure.HealthChecks;

public sealed class Bank1ExternalBankHealthCheck : IHealthCheck
{
    private readonly Bank1ExternalApiClient _externalApiClient;

    public Bank1ExternalBankHealthCheck(Bank1ExternalApiClient externalApiClient) =>
        _externalApiClient = externalApiClient;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await _externalApiClient.PingAsync(cancellationToken);
        return isHealthy
            ? HealthCheckResult.Healthy("External Bank1 API is reachable.")
            : HealthCheckResult.Degraded("External Bank1 API is not reachable.");
    }
}
