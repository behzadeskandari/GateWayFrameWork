using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Background;

public sealed class Bank2ReconciliationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Bank2ReconciliationOptions _options;
    private readonly ILogger<Bank2ReconciliationHostedService> _logger;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public Bank2ReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<Bank2ReconciliationOptions> options,
        ILogger<Bank2ReconciliationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(_options.IntervalSeconds, 5)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!await _executionLock.WaitAsync(0, stoppingToken))
            {
                _logger.LogDebug("Skipped reconciliation tick because a previous run is still active.");
                continue;
            }

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
                await reconciliationService.ReconcilePaymentsAsync(stoppingToken);
                await reconciliationService.ReconcileTransfersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bank2 reconciliation worker encountered an unexpected error.");
            }
            finally
            {
                _executionLock.Release();
            }
        }
    }
}
