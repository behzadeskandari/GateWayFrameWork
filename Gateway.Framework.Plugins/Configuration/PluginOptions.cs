namespace Gateway.Framework.Plugins.Configuration;

public class PluginOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public static class PluginOptionsValidator
{
    public static void Validate(string pluginKey, PluginOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException($"Plugin '{pluginKey}' is enabled but BaseUrl is missing.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Plugin '{pluginKey}' has an invalid BaseUrl.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException($"Plugin '{pluginKey}' TimeoutSeconds must be greater than zero.");
        }
    }

    public static void ValidateProduction(string pluginKey, PluginOptions options)
    {
        Validate(pluginKey, options);

        if (!options.Enabled)
        {
            return;
        }

        if (options.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Production cannot use localhost BaseUrl for enabled plugin '{pluginKey}'.");
        }
    }
}
