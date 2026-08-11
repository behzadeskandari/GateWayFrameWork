# Plugin Development Guide

## Creating a New Bank Plugin

### 1. Create a project

```
plugins/
  YourBank/
    Gateway.Bank.YourBank/
      Gateway.Bank.YourBank.csproj
```

Reference only `Gateway.Framework.Plugins` (which pulls in framework abstractions).

```xml
<ProjectReference Include="..\..\..\Gateway.Framework.Plugins\Gateway.Framework.Plugins.csproj" />
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

### 2. Implement `IBankingGatewayPlugin`

```csharp
public sealed class YourBankPlugin : IBankingGatewayPlugin
{
    public BankingGatewayPluginMetadata Metadata { get; } = new(
        Name: "Your Bank",
        BankCode: "YOURBANK",
        Version: "1.0.0",
        FrameworkVersion: BankingGatewayPluginManager.FrameworkVersion,
        ConfigurationKey: "YourBank",
        Capabilities: BankingPluginCapability.Accounts | BankingPluginCapability.Payment);

    public bool IsEnabled(IConfiguration configuration) =>
        configuration.GetSection("Plugins:YourBank").Get<YourBankOptions>()?.Enabled ?? false;

    public void ConfigureServices(BankingGatewayPluginContext context)
    {
        context.Services.Configure<YourBankOptions>(context.Configuration);
        context.AddBankPluginHttpClient<YourBankClient>("yourbank-client");
        context.Services.AddSingleton<IYourBankService, YourBankService>();
        context.Services.AddHealthChecks()
            .AddTypeActivatedCheck<YourBankHealthCheck>(
                "plugin-yourbank",
                failureStatus: null,
                tags: [HealthTags.Plugin, HealthTags.Ready],
                context.Configuration);
    }

    public void ConfigureRoutes(PluginRouteBuilder routes, IConfiguration pluginConfiguration)
    {
        var options = pluginConfiguration.Get<YourBankOptions>() ?? new YourBankOptions();
        routes.AddRoute(
            routeSuffix: "accounts",
            path: "/api/v1/banks/yourbank/accounts/{**catch-all}",
            destinationAddress: options.BaseUrl,
            pathRemovePrefix: "/api/v1/banks/yourbank/accounts");
    }

    public Task InitializeAsync(BankingGatewayPluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
```

### 3. Register in `Gateway.Host/Program.cs`

```csharp
builder.Services.AddBankingGatewayPlugins(builder.Configuration, plugins =>
{
    plugins.AddPlugin<Bank1Plugin>();
    plugins.AddPlugin<Bank2Plugin>();
    plugins.AddPlugin<YourBankPlugin>();
});
```

Add a project reference from `Gateway.Host` to your plugin project.

### 4. Configure in `appsettings.json`

```json
"Plugins": {
  "YourBank": {
    "Enabled": true,
    "BaseUrl": "https://api.yourbank.example/",
    "TimeoutSeconds": 30
  }
}
```

Production requires non-localhost `BaseUrl` for enabled plugins.

## Plugin Capabilities

Declare supported capabilities using `BankingPluginCapability` flags:

| Capability | Description |
|---|---|
| Accounts | Account inquiry |
| Balance | Balance lookup |
| Payment | Payment initiation |
| Transfer | Fund transfers |
| Cheque | Cheque operations |
| Card | Card services |
| Statement | Statements |
| Customer | Customer profile |
| Transaction | Transaction history |

Not every bank must implement every capability.

## HttpClient Rules

- **Always** use `context.AddBankPluginHttpClient<T>()` — never `new HttpClient()`
- Correlation IDs are propagated automatically
- Set `financialOperations: true` for payment/transfer clients
- Use typed clients (`Bank1AccountsClient` pattern)

## YARP Routes

Plugins declare routes through `PluginRouteBuilder`. You **cannot** supply arbitrary YARP JSON — the framework validates and merges routes safely.

Financial routes should set `requiresFinancialResilience: true`.

## Health Checks

Register per-plugin health checks in `ConfigureServices`. They appear in `/health/ready` with tag `plugin`.

Liveness (`/health/live`) never depends on downstream banks.

## Testing

- Unit test plugin registration with `AddBankingGatewayPlugins`
- Integration test routes via `WebApplicationFactory<Program>`
- Do not require production IdP or bank APIs — use sample/mock downstream URLs

## Dependency Rule

```
Gateway.Bank.YourBank  →  Gateway.Framework.Plugins  →  Gateway.Framework.*
```

Never add bank references to framework projects.
