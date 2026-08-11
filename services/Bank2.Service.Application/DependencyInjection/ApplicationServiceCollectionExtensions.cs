using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Features.Payments.CreatePayment;
using Bank2.Service.Application.Features.Payments.GetPayments;
using Bank2.Service.Application.Features.Transfers.CreateTransfer;
using Bank2.Service.Application.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bank2.Service.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBank2Application(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Bank2Options>(configuration.GetSection(Bank2Options.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuditDatabaseOptions>(configuration.GetSection(AuditDatabaseOptions.SectionName));
        services.Configure<Bank2ProxyOptions>(configuration.GetSection(Bank2ProxyOptions.SectionName));

        services.AddValidatorsFromAssemblyContaining<CreatePaymentValidator>();

        services.AddScoped<IGetPaymentsHandler, GetPaymentsHandler>();
        services.AddScoped<ICreatePaymentHandler, CreatePaymentHandler>();
        services.AddScoped<ICreateTransferHandler, CreateTransferHandler>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
