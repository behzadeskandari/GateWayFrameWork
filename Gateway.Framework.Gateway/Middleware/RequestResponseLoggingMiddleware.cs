using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Logging.Serilog.Masking;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Gateway.Middleware;

public sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly LogMaskingOptions _maskingOptions;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<LogMaskingOptions> maskingOptions)
    {
        _next = next;
        _logger = logger;
        _maskingOptions = maskingOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        var requestPath = SensitiveDataMasker.Mask(context.Request.Path.Value, _maskingOptions);
        _logger.LogInformation(
            "HTTP {Method} {Path} started correlationId={CorrelationId}",
            context.Request.Method,
            requestPath,
            correlationIdAccessor.CorrelationId);

        var started = DateTimeOffset.UtcNow;
        await _next(context);
        var elapsed = DateTimeOffset.UtcNow - started;

        _logger.LogInformation(
            "HTTP {Method} {Path} completed status={StatusCode} elapsedMs={ElapsedMs} correlationId={CorrelationId}",
            context.Request.Method,
            requestPath,
            context.Response.StatusCode,
            elapsed.TotalMilliseconds,
            correlationIdAccessor.CorrelationId);
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayRequestResponseLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
}
