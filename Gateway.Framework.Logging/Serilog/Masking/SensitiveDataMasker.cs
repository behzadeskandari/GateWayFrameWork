using System.Text.RegularExpressions;

namespace Gateway.Framework.Logging.Serilog.Masking;

public static partial class SensitiveDataMasker
{
    private static readonly string[] SensitiveHeaderNames =
    [
        "authorization", "cookie", "set-cookie", "x-api-key", "x-client-secret"
    ];

    public static string Mask(string? input, LogMaskingOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        var masked = BearerPattern().Replace(input, $"Bearer {options.MaskToken}");
        masked = AuthorizationHeaderPattern().Replace(masked, $"Authorization: {options.MaskToken}");
        masked = PasswordPattern().Replace(masked, $"password={options.MaskToken}");
        masked = CookiePattern().Replace(masked, options.MaskToken);
        masked = CreditCardPattern().Replace(masked, options.MaskToken);
        masked = JwtPattern().Replace(masked, options.MaskToken);
        masked = ClientSecretPattern().Replace(masked, options.MaskToken);

        foreach (var property in options.SensitivePropertyNames)
        {
            masked = PropertyPattern(property).Replace(masked, $"{property}={options.MaskToken}");
        }

        return masked;
    }

    public static string MaskHeader(string headerName, string? value, LogMaskingOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        if (SensitiveHeaderNames.Contains(headerName, StringComparer.OrdinalIgnoreCase) ||
            options.SensitivePropertyNames.Contains(headerName, StringComparer.OrdinalIgnoreCase))
        {
            return options.MaskToken;
        }

        return Mask(value, options);
    }

    [GeneratedRegex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex CreditCardPattern();

    [GeneratedRegex(@"eyJ[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.?[A-Za-z0-9-_.+/=]*", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"Authorization\s*:\s*[^\s,;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AuthorizationHeaderPattern();

    [GeneratedRegex(@"(?i)(password|passwd|pwd)\s*[=:]\s*\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"(?i)(cookie|set-cookie)\s*[=:]\s*[^;]+", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex CookiePattern();

    [GeneratedRegex(@"(?i)(client_secret|client-secret)\s*[=:]\s*\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ClientSecretPattern();

    private static Regex PropertyPattern(string propertyName) =>
        new($"(?i){Regex.Escape(propertyName)}\\s*[=:]\\s*\\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
