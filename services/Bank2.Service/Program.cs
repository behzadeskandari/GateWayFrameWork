using Bank2.Service.Application.DependencyInjection;
using Bank2.Service.Infrastructure.DependencyInjection;
using Bank2.Service.Infrastructure.Persistence;
using Bank2.Service.Middleware;
using Microsoft.OpenApi.Models;

namespace Bank2.Service;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddBank2Application(builder.Configuration);
        builder.Services.AddBank2Infrastructure(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Bank2 Sample Service",
                Version = "v1",
                Description = "Demonstration bank integration service. Does not execute real banking transactions."
            });
        });
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Bank2 service is running."), tags: ["live"]);
        builder.Services.AddHostedService<Background.Bank2ReconciliationHostedService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank2 Sample Service v1"));
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.UseMiddleware<FinancialIdempotencyRequiredMiddleware>();
        app.UseMiddleware<AuditMiddleware>();
        app.MapControllers();
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        await app.RunAsync();
    }
}
