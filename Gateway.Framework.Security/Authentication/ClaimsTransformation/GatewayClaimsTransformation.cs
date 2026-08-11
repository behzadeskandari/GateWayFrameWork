using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Gateway.Framework.Security.Authentication.ClaimsTransformation;

public sealed class GatewayClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        if (!identity.HasClaim(c => c.Type == "gateway_role"))
        {
            var role = identity.FindFirst(ClaimTypes.Role)?.Value ?? "customer";
            identity.AddClaim(new Claim("gateway_role", role));
        }

        return Task.FromResult(principal);
    }
}
