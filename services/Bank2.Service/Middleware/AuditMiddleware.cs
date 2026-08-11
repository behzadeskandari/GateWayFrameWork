using Bank2.Service.Application.Abstractions;

namespace Bank2.Service.Middleware;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var operation = ResolveOperation(context.Request.Method, context.Request.Path);
        var (resourceType, resourceId) = ResolveResource(context.Request.Path);

        try
        {
            await _next(context);

            var success = context.Response.StatusCode < StatusCodes.Status400BadRequest;
            await auditService.WriteOperationAsync(
                operation,
                resourceType: resourceType,
                resourceId: resourceId,
                success: success,
                correlationId: correlationId,
                errorCode: success ? null : context.Response.StatusCode.ToString(),
                context.RequestAborted);
        }
        catch (Exception)
        {
            await auditService.WriteOperationAsync(
                operation,
                resourceType: resourceType,
                resourceId: resourceId,
                success: false,
                correlationId: correlationId,
                errorCode: "UnhandledException",
                context.RequestAborted);
            throw;
        }
    }

    private static string ResolveOperation(string method, PathString path)
    {
        var value = path.Value ?? string.Empty;

        if (string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase) &&
            value.Contains("/payments", StringComparison.OrdinalIgnoreCase))
        {
            return "ListPayments";
        }

        if (string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase) &&
            value.Contains("/payments", StringComparison.OrdinalIgnoreCase))
        {
            return "CreatePayment";
        }

        if (string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase) &&
            value.Contains("/transfers", StringComparison.OrdinalIgnoreCase))
        {
            return "CreateTransfer";
        }

        return $"{method} {path}";
    }

    private static (string? ResourceType, string? ResourceId) ResolveResource(PathString path)
    {
        var value = path.Value ?? string.Empty;

        if (value.Contains("/transfers", StringComparison.OrdinalIgnoreCase))
        {
            return ("Transfer", null);
        }

        if (value.Contains("/payments", StringComparison.OrdinalIgnoreCase))
        {
            return ("Payment", null);
        }

        return (null, null);
    }
}
