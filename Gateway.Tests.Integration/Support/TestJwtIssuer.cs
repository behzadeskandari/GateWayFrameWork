using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Tests.Integration.Support;

public static class TestJwtIssuer
{
    public const string Issuer = "https://test-issuer.local/";
    public const string Audience = "gateway-api";
    public const string RequiredScope = "gateway.access";
    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("integration-test-signing-key-32bytes!"));

    public static string CreateToken(
        string subject = "user-123",
        string? role = "operator",
        string? scope = RequiredScope,
        TimeSpan? lifetime = null,
        string? signingKeyOverride = null,
        string? issuer = null,
        string? audience = null)
    {
        var credentials = signingKeyOverride is null
            ? new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
            : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyOverride)), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new("sub", subject) };
        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim("role", role));
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(5)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateExpiredToken() => CreateToken(lifetime: TimeSpan.FromMinutes(-5));

    public static TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
}
