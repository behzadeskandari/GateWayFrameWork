using Gateway.Framework.Logging.Audit;
using Gateway.Framework.Security.Authentication;
using Gateway.Framework.Security.Authentication.ClaimsTransformation;
using Gateway.Framework.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Framework.Security.TokenValidation;

public static class TokenValidationExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateOnStart()
            .Services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
        services.AddSingleton<IIdentityProvider, ExternalIdentityProvider>();
        services.AddSingleton<ITokenValidator, JwtBearerTokenValidator>();
        services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, GatewayClaimsTransformation>();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        if (!authOptions.Enabled)
        {
            services.AddAuthorizationBuilder();
            return services;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.Audience = authOptions.Audience;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = authOptions.ValidateIssuer,
                    ValidateAudience = authOptions.ValidateAudience,
                    ValidateLifetime = authOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = authOptions.ValidateIssuerSigningKey,
                    ValidAudience = authOptions.Audience,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var auditLogger = context.HttpContext.RequestServices.GetService<IAuditLogger>();
                        if (auditLogger is not null)
                        {
                            await auditLogger.LogAsync(
                                AuditActions.AuthenticationSuccess,
                                context.HttpContext.Request.Path,
                                AuditOutcomes.Success,
                                AuditContext.FromHttpContext(context.HttpContext));
                        }
                    },
                    OnAuthenticationFailed = async context =>
                    {
                        var auditLogger = context.HttpContext.RequestServices.GetService<IAuditLogger>();
                        if (auditLogger is not null)
                        {
                            var metadata = AuditContext.FromHttpContext(context.HttpContext);
                            metadata["reason"] = context.Exception.GetType().Name;
                            await auditLogger.LogAsync(
                                AuditActions.AuthenticationFailure,
                                context.HttpContext.Request.Path,
                                AuditOutcomes.Failure,
                                metadata);
                        }

                        context.Fail("Authentication failed.");
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"message\":\"Unauthorized\"}");
                    }
                };
            });

        var authorization = services.AddAuthorizationBuilder()
            .AddPolicy(GatewayPolicies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(GatewayPolicies.BankingOperator, policy => policy.RequireRole("operator", "admin"))
            .AddPolicy(GatewayPolicies.BankingAdmin, policy => policy.RequireRole("admin"));

        if (authOptions.ValidateScopes && authOptions.RequiredScopes.Length > 0)
        {
            authorization.AddPolicy(
                GatewayPolicies.RequiredScopes,
                policy => policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope")?.Value
                                     ?? context.User.FindFirst("scp")?.Value
                                     ?? string.Empty;
                    var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return authOptions.RequiredScopes.All(required => scopes.Contains(required));
                }));
        }

        return services;
    }

    public static bool IsAuthenticationRequired(IHostEnvironment environment, AuthOptions options)
    {
        if (environment.IsDevelopment() && options.AllowDevelopmentAnonymous)
        {
            return false;
        }

        return options.Enabled;
    }
}
