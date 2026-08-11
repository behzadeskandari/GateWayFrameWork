using Asp.Versioning;
using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Responses;
using Gateway.Framework.Plugins.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Host.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/health")]
[ApiVersion("1.0")]
public sealed class HealthStatusController : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetStatus(
        [FromServices] ICorrelationIdAccessor correlationIdAccessor,
        [FromServices] IBankingGatewayPluginManager pluginManager)
    {
        var payload = new
        {
            Service = "Gateway.Host",
            Status = "Running",
            Timestamp = DateTimeOffset.UtcNow,
            Plugins = pluginManager.GetPluginStatuses().Select(status => new
            {
                status.BankCode,
                status.Name,
                status.Version,
                status.State,
                status.Enabled,
                Capabilities = status.Capabilities.ToString(),
                status.Error
            }),
            AvailableCapabilities = pluginManager.GetAvailableCapabilities().Select(c => c.ToString())
        };

        return Ok(ApiResponse<object>.Ok(payload, correlationIdAccessor.CorrelationId));
    }
}
