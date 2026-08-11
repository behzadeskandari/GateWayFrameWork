using Gateway.Framework.Logging.Audit;
using Gateway.Framework.Logging.Serilog.Masking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Gateway.Framework.Logging.Serilog;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddGatewaySerilog(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<LogMaskingOptions>(builder.Configuration.GetSection(LogMaskingOptions.SectionName));

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console();
        });

        builder.Services.AddScoped<IAuditLogger, AuditLogger>();
        return builder;
    }
}
