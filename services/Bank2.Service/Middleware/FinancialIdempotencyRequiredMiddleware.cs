using Bank2.Service.Application.Configuration;
using Bank2.Service.Middleware;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Middleware;

public sealed class FinancialIdempotencyRequiredMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Bank2ProxyOptions _proxyOptions;

    public FinancialIdempotencyRequiredMiddleware(RequestDelegate next, IOptions<Bank2ProxyOptions> proxyOptions)
    {
        _next = next;
        _proxyOptions = proxyOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_proxyOptions.Enabled && IsFinancialPost(context.Request))
        {
            var idempotencyKey = context.Items[IdempotencyMiddleware.IdempotencyKeyItemKey] as string;
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    Title = "Idempotency-Key header is required for financial operations when external proxy is enabled.",
                    Status = StatusCodes.Status400BadRequest,
                    ErrorCode = "IdempotencyKeyRequired"
                });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsFinancialPost(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? string.Empty;
        return path.Contains("/payments", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/transfers", StringComparison.OrdinalIgnoreCase);
    }
}
