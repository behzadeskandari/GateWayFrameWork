using Gateway.Framework.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Gateway.Framework.Logging.Audit;

public static class AuditContext
{
    public static Dictionary<string, string> FromHttpContext(HttpContext context, ICorrelationIdAccessor? correlationIdAccessor = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["method"] = context.Request.Method,
            ["path"] = context.Request.Path.Value ?? "/",
            ["statusCode"] = context.Response.StatusCode.ToString(),
            ["requestId"] = context.TraceIdentifier
        };

        if (correlationIdAccessor?.CorrelationId is { } correlationId)
        {
            metadata["correlationId"] = correlationId;
        }

        var subject = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            metadata["subject"] = subject;
        }

        var clientId = context.User.FindFirst("client_id")?.Value ?? context.User.FindFirst("azp")?.Value;
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            metadata["clientId"] = clientId;
        }

        var sourceIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(sourceIp))
        {
            metadata["sourceIp"] = sourceIp;
        }

        return metadata;
    }
}
