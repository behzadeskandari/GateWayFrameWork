using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Gateway.Framework.Resilience;

public static class ResilienceExtensions
{
    public const string FinancialHttpClientName = "gateway-financial";

    public static IServiceCollection AddGatewayResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        var options = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(resilience =>
            {
                ConfigureSafeRetry(resilience.Retry, options.MaxRetryAttempts, options.RetryDelayMilliseconds);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingDurationSeconds);
                resilience.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
                resilience.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
                resilience.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalRequestTimeoutSeconds);
            });
        });

        services.AddHttpClient(FinancialHttpClientName)
            .AddStandardResilienceHandler(resilience =>
            {
                ConfigureSafeRetry(resilience.Retry, options.FinancialMaxRetryAttempts, options.RetryDelayMilliseconds);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingDurationSeconds);
                resilience.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
                resilience.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
                resilience.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalRequestTimeoutSeconds);
            });

        return services;
    }

    private static void ConfigureSafeRetry(HttpRetryStrategyOptions retry, int maxAttempts, int delayMs)
    {
        if (maxAttempts <= 0)
        {
            retry.MaxRetryAttempts = 1;
            retry.ShouldHandle = _ => ValueTask.FromResult(false);
            return;
        }

        retry.MaxRetryAttempts = maxAttempts;
        retry.Delay = TimeSpan.FromMilliseconds(delayMs);
        retry.ShouldHandle = args =>
        {
            var method = args.Outcome.Result?.RequestMessage?.Method;
            if (method is null)
            {
                return ValueTask.FromResult(false);
            }

            var isSafeMethod = HttpMethod.Get == method ||
                               HttpMethod.Head == method ||
                               HttpMethod.Options == method ||
                               HttpMethod.Trace == method;

            return ValueTask.FromResult(isSafeMethod);
        };
    }
}
