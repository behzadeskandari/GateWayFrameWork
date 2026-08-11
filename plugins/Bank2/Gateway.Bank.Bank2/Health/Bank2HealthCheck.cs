using Gateway.Framework.Plugins.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Bank.Bank2.Health;

public sealed class Bank2HealthCheck : IHealthCheck
{
    private readonly Bank2PaymentsClient _client;
    private readonly IConfiguration _configuration;

    public Bank2HealthCheck(Bank2PaymentsClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _configuration.Get<Bank2Options>() ?? new Bank2Options();
        if (!options.Enabled)
        {
            return HealthCheckResult.Healthy("Bank2 plugin disabled.");
        }

        try
        {
            using var response = await _client.Client.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Bank2 sample downstream is reachable.")
                : HealthCheckResult.Degraded($"Bank2 sample downstream returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Bank2 sample downstream is unavailable.", ex);
        }
    }
}
