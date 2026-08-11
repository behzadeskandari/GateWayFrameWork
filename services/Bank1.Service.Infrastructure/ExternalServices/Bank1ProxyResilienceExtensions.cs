using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Bank1.Service.Infrastructure.ExternalServices;

public static class Bank1ProxyResilienceExtensions
{
    public static IHttpClientBuilder AddBank1ProxyResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.ShouldHandle = static args =>
            {
                if (args.Outcome.Result?.RequestMessage?.Method != HttpMethod.Get)
                {
                    return ValueTask.FromResult(false);
                }

                return ValueTask.FromResult(args.Outcome.Result is { IsSuccessStatusCode: false });
            };
        });

        return builder;
    }
}
