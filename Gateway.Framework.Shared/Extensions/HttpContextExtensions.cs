using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Observability.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Shared.Extensions;

public static class HttpContextExtensions
{
    public const string CorrelationIdItemKey = "Gateway.CorrelationId";

    public static string GetOrCreateCorrelationId(this HttpContext context, CorrelationIdOptions options)
    {
        if (context.Items.TryGetValue(CorrelationIdItemKey, out var existing) &&
            existing is string existingId &&
            !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId;
        }

        var correlationId = context.Request.Headers.TryGetValue(options.HeaderName, out var headerValue) &&
                            !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[CorrelationIdItemKey] = correlationId;
        return correlationId;
    }

    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdItemKey, out var value) ? value as string : null;

    public static void SetCorrelationIdAccessor(
        this HttpContext context,
        ICorrelationIdAccessor accessor,
        IOptions<CorrelationIdOptions> options)
    {
        accessor.CorrelationId = context.GetOrCreateCorrelationId(options.Value);
    }
}
