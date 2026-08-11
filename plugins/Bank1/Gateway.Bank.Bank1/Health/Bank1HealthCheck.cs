using Gateway.Bank.Bank1;
using Gateway.Framework.Plugins.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Bank.Bank1.Health;

public sealed class Bank1HealthCheck : IHealthCheck
{
    private readonly Bank1AccountsClient _client;
    private readonly IConfiguration _configuration;

    public Bank1HealthCheck(Bank1AccountsClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _configuration.Get<Bank1Options>() ?? new Bank1Options();
        if (!options.Enabled)
        {
            return HealthCheckResult.Healthy("Bank1 plugin disabled.");
        }

        try
        {
            using var response = await _client.Client.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Bank1 sample downstream is reachable.")
                : HealthCheckResult.Degraded($"Bank1 sample downstream returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Bank1 sample downstream is unavailable.", ex);
        }
    }
}
