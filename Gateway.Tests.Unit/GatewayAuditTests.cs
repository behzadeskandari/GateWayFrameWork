using Gateway.Framework.Gateway.Middleware;
using Gateway.Framework.Security.Authentication;
using Gateway.Framework.Security.Middleware;
using Gateway.Framework.Shared.Extensions;
using Gateway.Framework.Core.Observability.Correlation;
using Gateway.Framework.Logging.Serilog.Masking;
using Gateway.Framework.Resilience;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gateway.Tests.Unit;

public sealed class AuthOptionsValidatorTests
{
    [Fact]
    public void Validate_RejectsDevelopmentAnonymousOutsideDevelopment()
    {
        var validator = new AuthOptionsValidator(new TestHostEnvironment("Production"));
        var result = validator.Validate(null, new AuthOptions { AllowDevelopmentAnonymous = true });
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_RequiresAuthorityInProductionWhenEnabled()
    {
        var validator = new AuthOptionsValidator(new TestHostEnvironment("Production"));
        var result = validator.Validate(null, new AuthOptions { Enabled = true, Audience = "api" });
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_SucceedsForValidProductionConfiguration()
    {
        var validator = new AuthOptionsValidator(new TestHostEnvironment("Production"));
        var result = validator.Validate(null, new AuthOptions
        {
            Enabled = true,
            Authority = "https://login.example.com/",
            Audience = "gateway-api",
            RequireHttpsMetadata = true
        });
        Assert.True(result.Succeeded);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}

public sealed class SensitiveDataMaskerTests
{
    private readonly LogMaskingOptions _options = new();

    [Fact]
    public void Mask_RedactsJwtTokens() =>
        Assert.Contains("***REDACTED***", SensitiveDataMasker.Mask("Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig", _options));

    [Fact]
    public void Mask_RedactsAuthorizationHeader() =>
        Assert.DoesNotContain("secret-token", SensitiveDataMasker.Mask("Authorization: secret-token", _options));

    [Fact]
    public void Mask_RedactsPasswords() =>
        Assert.DoesNotContain("hunter2", SensitiveDataMasker.Mask("password=hunter2", _options));

    [Fact]
    public void MaskHeader_RedactsAuthorizationHeaderName() =>
        Assert.Equal("***REDACTED***", SensitiveDataMasker.MaskHeader("Authorization", "Bearer abc", _options));

    [Fact]
    public void Mask_ReturnsEmptyWhenInputIsNull() =>
        Assert.Equal(string.Empty, SensitiveDataMasker.Mask(null, _options));
}

public sealed class IpAllowListMiddlewareTests
{
    [Fact]
    public void ResolveClientIp_UsesRemoteIpAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.10");
        var ip = IpAllowListMiddleware.ResolveClientIp(context);
        Assert.Equal("203.0.113.10", ip);
    }
}

public sealed class CorrelationIdExtensionsTests
{
    [Fact]
    public void GetOrCreateCorrelationId_GeneratesValueWhenMissing()
    {
        var context = new DefaultHttpContext();
        var options = new CorrelationIdOptions();
        var id = context.GetOrCreateCorrelationId(options);
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(id, context.GetCorrelationId());
    }

    [Fact]
    public void GetOrCreateCorrelationId_UsesIncomingHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "abc123";
        var id = context.GetOrCreateCorrelationId(new CorrelationIdOptions());
        Assert.Equal("abc123", id);
    }
}

public sealed class ResilienceOptionsTests
{
    [Fact]
    public void Defaults_DisableFinancialRetries()
    {
        var options = new ResilienceOptions();
        Assert.Equal(0, options.FinancialMaxRetryAttempts);
        Assert.Contains("payments-cluster", options.FinancialClusterIds);
        Assert.True(options.DisableRetryOnUnsafeHttpMethods);
    }
}

public sealed class InputValidationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RejectsSuspiciousHeaderCharacters()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test"] = "<script>";
        var middleware = new InputValidationMiddleware(_ => Task.CompletedTask, new InputValidationOptions());

        await Assert.ThrowsAsync<Gateway.Framework.Core.Errors.GatewayValidationException>(
            () => middleware.InvokeAsync(context));
    }
}
