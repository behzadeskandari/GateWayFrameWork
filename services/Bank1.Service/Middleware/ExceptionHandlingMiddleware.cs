using Banking.Service.External.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Bank1.Service.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteErrorResponseAsync(context, exception);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errorCode) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed for {Method} {Path}.", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            Title = title,
            Status = statusCode,
            ErrorCode = errorCode,
            TraceId = context.TraceIdentifier,
            Detail = _environment.IsDevelopment() ? exception.Message : null
        };

        await context.Response.WriteAsJsonAsync(payload);
    }

    private static (int StatusCode, string Title, string ErrorCode) MapException(Exception exception) =>
        exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed.", "ValidationFailed"),
            ExternalBankAuthenticationException => (StatusCodes.Status502BadGateway, "External bank authentication failed.", "ExternalBankAuthenticationFailed"),
            ExternalBankTimeoutException => (StatusCodes.Status504GatewayTimeout, "External bank request timed out.", "ExternalBankTimeout"),
            ExternalBankUnavailableException => (StatusCodes.Status503ServiceUnavailable, "External bank is unavailable.", "ExternalBankUnavailable"),
            ExternalBankResponseException responseException => (MapExternalStatus(responseException.StatusCode), "External bank rejected the request.", responseException.ErrorCode ?? "ExternalBankError"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "InternalError")
        };

    private static int MapExternalStatus(int externalStatusCode) =>
        externalStatusCode switch
        {
            StatusCodes.Status400BadRequest => StatusCodes.Status502BadGateway,
            StatusCodes.Status404NotFound => StatusCodes.Status404NotFound,
            StatusCodes.Status409Conflict => StatusCodes.Status409Conflict,
            _ when externalStatusCode >= 500 => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status502BadGateway
        };
}
