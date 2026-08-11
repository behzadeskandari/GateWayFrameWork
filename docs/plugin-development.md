# Plugin Development Guide

## Plugin vs Bank Service

When adding a new bank integration, you typically create **two** projects:

| Project | Location | Purpose |
|---|---|---|
| Plugin | `plugins/YourBank/Gateway.Bank.YourBank/` | Gateway-side routing, health, HttpClient wiring |
| Service | `services/YourBank.Service/` | Standalone ASP.NET Core app with bank API + business logic |

The plugin's `Plugins:YourBank:BaseUrl` must point to the service URL (e.g. `http://localhost:5103/` in development, internal K8s URL in production).

```
Client → Gateway → Plugin → YourBank.Service → (future) Real Bank API
```

The plugin **never** contains business logic. The service **never** duplicates gateway JWT validation.

---

## Creating a New Bank Plugin

### 1. Create a plugin project

```
plugins/
  YourBank/
    Gateway.Bank.YourBank/
      Gateway.Bank.YourBank.csproj
```

Reference only `Gateway.Framework.Plugins`:

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
            routeSuffix: "yourbank",
            path: "/api/v1/banks/yourbank/{**catch-all}",
            destinationAddress: options.BaseUrl,
            pathRemovePrefix: "/api/v1/banks/yourbank",
            pathPrefix: "/api");
    }

    public Task InitializeAsync(BankingGatewayPluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
```

**Route transforms:** Gateway clients call `/api/v1/banks/yourbank/...`. The plugin strips the gateway prefix and adds `/api` so the service receives its native paths (e.g. `/api/accounts`).

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
    "BaseUrl": "http://localhost:5103/",
    "TimeoutSeconds": 30
  }
}
```

Production requires non-localhost `BaseUrl` for enabled plugins.

---

## Creating a Matching Bank Service

Create an independent .NET 8 ASP.NET Core project under `services/YourBank.Service/`:

```
services/YourBank.Service/
  Program.cs
  Controllers/
  Application/
  Infrastructure/
  Properties/launchSettings.json
  appsettings.json
  Dockerfile
services/YourBank.Service.Tests/
```

Each service should include:

- Its own DI, controllers, application/infrastructure layers
- Swagger at `/swagger` (Development)
- Health at `/health/live` and `/health/ready`
- `CorrelationIdMiddleware` echoing `X-Correlation-Id`
- Its own port in `launchSettings.json`
- Unit + integration tests using `WebApplicationFactory<YourBank.Service.Program>`

**Do not** add IdentityServer, token issuance, or gateway JWT validation to the service.

---

## Running and Testing

### Start locally

```bash
# Terminal 1 — your bank service
dotnet run --project services/YourBank.Service/YourBank.Service.csproj

# Terminal 2 — gateway (after updating Plugins:YourBank:BaseUrl)
dotnet run --project Gateway.Host/Gateway.Host.csproj
```

### Test direct service (Swagger)

Open `http://localhost:{port}/swagger` and exercise service endpoints directly.

### Test through gateway

```bash
curl http://localhost:5000/api/v1/banks/yourbank/{your-endpoint}
```

With auth enabled, include a valid Bearer token from your external IdP.

### Verify plugin health

```bash
curl http://localhost:5000/api/v1/health/status
```

Plugin health checks call `{BaseUrl}health/live` on your service.

---

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
- Use typed clients for health checks and auxiliary calls

## YARP Routes

Plugins declare routes through `PluginRouteBuilder`. You **cannot** supply arbitrary YARP JSON — the framework validates and merges routes safely.

Financial routes should set `requiresFinancialResilience: true`.

## Health Checks

Register per-plugin health checks in `ConfigureServices`. They appear in `/health/ready` with tag `plugin`.

The health check should call the **service** at `health/live`, not the gateway.

Liveness (`/health/live`) on the gateway never depends on downstream banks.

## Testing

- Unit test plugin registration with `AddBankingGatewayPlugins`
- Unit/integration test the bank service independently
- Gateway integration tests proxy to in-process bank service hosts via `Gateway.Tests.Integration`
- Do not require production IdP or real bank APIs for automated tests

## Dependency Rule

```
Gateway.Bank.YourBank  →  Gateway.Framework.Plugins  →  Gateway.Framework.*
services/YourBank.Service  →  (standalone, no gateway references)
```

Never add bank references to framework projects.

## Production

- Bank services: internal cluster DNS only (e.g. `http://yourbank-service.bank-namespace.svc.cluster.local/`)
- Gateway ingress: public-facing
- `Plugins:YourBank:BaseUrl`: internal service URL
- No hardcoded secrets in source or appsettings committed to git
