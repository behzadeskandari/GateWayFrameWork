using Gateway.Framework.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Tests.Integration.Support;

[ApiController]
[Route("integration-test")]
public sealed class AuthorizationTestController : ControllerBase
{
    [HttpGet("operator")]
    [Authorize(Policy = GatewayPolicies.BankingOperator)]
    public IActionResult OperatorOnly() => Ok();

    [HttpGet("authenticated")]
    [Authorize(Policy = GatewayPolicies.AuthenticatedUser)]
    public IActionResult AuthenticatedOnly() => Ok();
}
