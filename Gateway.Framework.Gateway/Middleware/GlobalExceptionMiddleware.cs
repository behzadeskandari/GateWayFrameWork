using Gateway.Framework.Core.Errors;
using System.Net;
using System.Text.Json;
using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Responses;
using Gateway.Framework.Infrastructure.Configuration;
using Gateway.Framework.Shared.Serialization;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Gateway.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly GatewayOptions _gatewayOptions;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IOptions<GatewayOptions> gatewayOptions)
    {
        _next = next;
        _logger = logger;
        _gatewayOptions = gatewayOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        try
        {
            await _next(context);
        }
        catch (GatewayValidationException validationException)
        {
            await WriteValidationErrorAsync(context, validationException, correlationIdAccessor.CorrelationId);
        }
        catch (SecurityException securityException)
        {
            var status = securityException.Code == ErrorCode.Unauthorized
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.Forbidden;
            await WriteErrorAsync(
                context,
                securityException.Code,
                securityException.Message,
                status,
                correlationIdAccessor.CorrelationId);
        }
        catch (UnauthorizedAccessException)
        {
            await WriteErrorAsync(
                context,
                ErrorCode.Unauthorized,
                "Unauthorized.",
                HttpStatusCode.Unauthorized,
                correlationIdAccessor.CorrelationId);
        }
        catch (DomainException domainException)
        {
            await WriteErrorAsync(
                context,
                domainException.Code,
                domainException.Message,
                HttpStatusCode.BadRequest,
                correlationIdAccessor.CorrelationId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled gateway exception.");
            await WriteErrorAsync(
                context,
                ErrorCode.InternalError,
                "An unexpected error occurred.",
                HttpStatusCode.InternalServerError,
                correlationIdAccessor.CorrelationId,
                _gatewayOptions.EnableDetailedErrors ? exception.GetType().Name : null);
        }
    }

    private static async Task WriteValidationErrorAsync(
        HttpContext context,
        GatewayValidationException exception,
        string? correlationId)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var payload = ApiValidationErrorResponse.From(exception, correlationId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonDefaults.Options));
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        ErrorCode code,
        string message,
        HttpStatusCode statusCode,
        string? correlationId,
        string? detail = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiErrorResponse.From(new BankingError(code, message, detail), correlationId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonDefaults.Options));
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
