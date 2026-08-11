using Gateway.Framework.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Gateway.Framework.Logging.Audit;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAuditLogger auditLogger, ICorrelationIdAccessor correlationIdAccessor)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await auditLogger.LogAsync(
                AuditActions.AuthenticationFailure,
                context.Request.Path,
                AuditOutcomes.Denied,
                AuditContext.FromHttpContext(context, correlationIdAccessor));
        }
        else if (context.Response.StatusCode == StatusCodes.Status403Forbidden &&
                 !context.Items.ContainsKey("ip_allow_list_rejected"))
        {
            await auditLogger.LogAsync(
                AuditActions.AuthorizationFailure,
                context.Request.Path,
                AuditOutcomes.Denied,
                AuditContext.FromHttpContext(context, correlationIdAccessor));
        }
    }
}

public static class AuditMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayAuditLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<AuditMiddleware>();
}
