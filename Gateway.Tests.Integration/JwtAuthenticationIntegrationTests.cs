using System.Net;
using System.Net.Http.Headers;
using Gateway.Host;
using Gateway.Tests.Integration.Support;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gateway.Tests.Integration;

public sealed class JwtAuthenticationIntegrationTests
{
    private HttpClient CreateAuthenticatedClient(bool validateScopes = true)
    {
        var factory = new WebApplicationFactory<GatewayHostApplicationMarker>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:Enabled", "true");
            builder.UseSetting("Auth:Authority", TestJwtIssuer.Issuer);
            builder.UseSetting("Auth:Audience", TestJwtIssuer.Audience);
            builder.UseSetting("Auth:RequiredScopes:0", TestJwtIssuer.RequiredScope);
            builder.UseSetting("Auth:ValidateScopes", validateScopes.ToString().ToLowerInvariant());
            builder.UseSetting("RateLimit:Enabled", "false");

            builder.ConfigureTestServices(services =>
            {
                services.AddControllers()
                    .ConfigureApplicationPartManager(manager =>
                    {
                        manager.ApplicationParts.Add(new AssemblyPart(typeof(AuthorizationTestController).Assembly));
                    });

                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = null;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = TestJwtIssuer.CreateValidationParameters();
                });
            });
        });

        return factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedRoute_WithValidToken_ReturnsNotUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtIssuer.CreateToken());

        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithoutToken_ReturnsUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithExpiredToken_ReturnsUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtIssuer.CreateExpiredToken());
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithInvalidSignature_ReturnsUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(signingKeyOverride: "wrong-signing-key-32-characters!!"));
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithInvalidIssuer_ReturnsUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(issuer: "https://wrong-issuer.local/"));
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithInvalidAudience_ReturnsUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(audience: "wrong-audience"));
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithMalformedToken_ReturnsUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithMissingScope_ReturnsForbidden()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(scope: "other.scope"));
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithValidScope_ReturnsNotUnauthorized()
    {
        using var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(scope: TestJwtIssuer.RequiredScope));
        var response = await client.GetAsync("/api/v1/accounts/test");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OperatorRoute_WithValidRole_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient(validateScopes: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(role: "operator", scope: null));
        var response = await client.GetAsync("/integration-test/operator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OperatorRoute_WithMissingRole_ReturnsForbidden()
    {
        using var client = CreateAuthenticatedClient(validateScopes: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtIssuer.CreateToken(role: null, scope: null));
        var response = await client.GetAsync("/integration-test/operator");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
