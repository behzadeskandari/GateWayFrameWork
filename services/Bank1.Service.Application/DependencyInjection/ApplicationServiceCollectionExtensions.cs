using Bank1.Service.Application.Abstractions;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Application.Features.Accounts.GetAccount;
using Bank1.Service.Application.Features.Accounts.GetAccounts;
using Bank1.Service.Application.Features.Accounts.GetBalance;
using Bank1.Service.Application.Services;
using Bank1.Service.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bank1.Service.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBank1Application(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Bank1Options>(configuration.GetSection(Bank1Options.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuditDatabaseOptions>(configuration.GetSection(AuditDatabaseOptions.SectionName));
        services.Configure<Bank1ProxyOptions>(configuration.GetSection(Bank1ProxyOptions.SectionName));

        services.AddValidatorsFromAssemblyContaining<GetAccountValidator>();

        services.AddScoped<IGetAccountsHandler, GetAccountsHandler>();
        services.AddScoped<IGetAccountHandler, GetAccountHandler>();
        services.AddScoped<IGetBalanceHandler, GetBalanceHandler>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
