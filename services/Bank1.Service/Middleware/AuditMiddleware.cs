using Bank1.Service.Application.Abstractions;

namespace Bank1.Service.Middleware;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var operation = ResolveOperation(context.Request.Method, context.Request.Path);
        var resourceId = context.Request.RouteValues.TryGetValue("id", out var id) ? id?.ToString() : null;

        try
        {
            await _next(context);

            var success = context.Response.StatusCode < StatusCodes.Status400BadRequest;
            await auditService.WriteOperationAsync(
                operation,
                resourceType: "Account",
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
                resourceType: "Account",
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
        if (!string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
        {
            return $"{method} {path}";
        }

        var value = path.Value ?? string.Empty;
        if (value.EndsWith("/balance", StringComparison.OrdinalIgnoreCase))
        {
            return "GetAccountBalance";
        }

        if (value.Count(c => c == '/') > 2)
        {
            return "GetAccount";
        }

        return "ListAccounts";
    }
}
