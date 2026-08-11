using Gateway.Framework.Core.Idempotency;
using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Observability.Correlation;
using Gateway.Framework.Shared.Extensions;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Gateway.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdOptions _options;

    public CorrelationIdMiddleware(RequestDelegate next, IOptions<CorrelationIdOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdAccessor correlationIdAccessor,
        IIdempotencyKeyAccessor idempotencyKeyAccessor)
    {
        var correlationId = context.GetOrCreateCorrelationId(_options);
        correlationIdAccessor.CorrelationId = correlationId;

        if (context.Request.Headers.TryGetValue(IdempotencyConstants.HeaderName, out var idempotencyKey) &&
            !string.IsNullOrWhiteSpace(idempotencyKey))
        {
            idempotencyKeyAccessor.Key = idempotencyKey.ToString();
        }

        if (_options.IncludeInResponse)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[_options.HeaderName] = correlationId;
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
