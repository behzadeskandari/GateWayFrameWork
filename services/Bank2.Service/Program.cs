using Bank2.Service.Application.Services;
using Bank2.Service.Infrastructure.Data;
using Bank2.Service.Middleware;
using Microsoft.OpenApi.Models;

namespace Bank2.Service;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
        builder.Services.AddSingleton<IPaymentService, PaymentService>();
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
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Bank2 service is running."), tags: ["live"])
            .AddCheck("ready", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Bank2 service is ready."), tags: ["ready"]);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank2 Sample Service v1"));
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.MapControllers();
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("live")
        });

        app.Run();
    }
}
