using Bank2.Service.Infrastructure.ExternalServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank2.Service.Infrastructure.HealthChecks;

internal sealed class Bank2ExternalBankHealthCheck : IHealthCheck
{
    private readonly Bank2ExternalApiClient _externalApiClient;

    public Bank2ExternalBankHealthCheck(Bank2ExternalApiClient externalApiClient) =>
        _externalApiClient = externalApiClient;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await _externalApiClient.PingAsync(cancellationToken);
        return isHealthy
            ? HealthCheckResult.Healthy("External Bank2 API is reachable.")
            : HealthCheckResult.Degraded("External Bank2 API is not reachable.");
    }
}
