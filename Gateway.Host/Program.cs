using Gateway.Bank.Bank1;
using Gateway.Bank.Bank2;
using Gateway.Framework.Gateway;
using Gateway.Framework.Infrastructure.Configuration;
using Gateway.Framework.Logging.Audit;
using Gateway.Framework.Logging.Serilog;
using Gateway.Framework.Monitoring.Health;
using Gateway.Framework.Monitoring.OpenTelemetry;
using Gateway.Framework.Plugins.Extensions;
using Gateway.Framework.Security.Authentication;
using Gateway.Framework.Security.Middleware;
using Gateway.Framework.Security.TokenValidation;
using Gateway.Framework.Shared.Extensions;
using Gateway.Host.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddGatewaySerilog();
builder.Services.AddGatewayCoreServices();
builder.Services.AddGatewayInfrastructure(builder.Configuration);
builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayIpAllowList(builder.Configuration);
builder.Services.AddGatewayRequestSizeLimit(builder.Configuration);
builder.Services.AddGatewayHealthChecks(builder.Configuration);
builder.Services.AddGatewayOpenTelemetry(builder.Configuration);
builder.Services.AddBankingGatewayPlugins(builder.Configuration, plugins =>
{
    plugins.AddPlugin<Bank1Plugin>();
    plugins.AddPlugin<Bank2Plugin>();
});
builder.Services.AddGatewayFramework(builder.Configuration);
builder.Services.AddControllers();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    var configured = ForwardedHeadersConfiguration.Configure(builder.Configuration);
    options.ForwardedHeaders = configured.ForwardedHeaders;
    options.KnownProxies.Clear();
    foreach (var proxy in configured.KnownProxies)
    {
        options.KnownProxies.Add(proxy);
    }

    options.KnownNetworks.Clear();
    foreach (var network in configured.KnownNetworks)
    {
        options.KnownNetworks.Add(network);
    }
});

var app = builder.Build();

ProductionConfigurationValidator.Validate(app);

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseGatewaySecureHeaders();
app.UseGatewayIpAllowList();
app.UseGatewayRequestSizeLimit();
app.UseGatewayMiddleware();

var authOptions = app.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
if (TokenValidationExtensions.IsAuthenticationRequired(app.Environment, authOptions))
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseGatewayAuditLogging();
}

app.MapGatewayEndpoints();

Log.Information(
    "Gateway.Host starting in {Environment}. AuthRequired={AuthRequired}",
    app.Environment.EnvironmentName,
    TokenValidationExtensions.IsAuthenticationRequired(app.Environment, authOptions));
app.Run();

public partial class Program;
