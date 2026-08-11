using System.Text.RegularExpressions;
using Gateway.Framework.Core.Errors;

namespace Gateway.Framework.Gateway.Middleware;

public sealed class InputValidationOptions
{
    public const string SectionName = "InputValidation";
    public bool Enabled { get; set; } = true;
    public int MaxHeaderLength { get; set; } = 4096;
}

public sealed partial class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly InputValidationOptions _options;

    public InputValidationMiddleware(RequestDelegate next, InputValidationOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        foreach (var header in context.Request.Headers)
        {
            if (header.Value.ToString().Length > _options.MaxHeaderLength)
            {
                throw new GatewayValidationException(
                    "Request headers failed validation.",
                    new Dictionary<string, string[]>
                    {
                        [header.Key] = ["Header value exceeds maximum allowed length."]
                    });
            }

            if (SuspiciousPattern().IsMatch(header.Value.ToString()))
            {
                throw new GatewayValidationException(
                    "Request headers failed validation.",
                    new Dictionary<string, string[]>
                    {
                        [header.Key] = ["Header contains disallowed characters."]
                    });
            }
        }

        await _next(context);
    }

    [GeneratedRegex(@"[<>""']", RegexOptions.Compiled)]
    private static partial Regex SuspiciousPattern();
}

public static class InputValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayInputValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<InputValidationMiddleware>();

    public static IServiceCollection AddGatewayInputValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InputValidationOptions>(configuration.GetSection(InputValidationOptions.SectionName));
        services.AddSingleton(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InputValidationOptions>>().Value);
        return services;
    }
}
