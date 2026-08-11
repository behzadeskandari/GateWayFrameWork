# GateWay Framework — .NET 8 Banking API Gateway

A modular **banking API gateway** on **.NET 8** and **YARP** with a **plugin architecture** for bank integrations. This document reflects the **current implementation**.

---

## Status Legend

| Label | Meaning |
|---|---|
| **IMPLEMENTED** | Wired in code, executed at runtime, covered by tests where critical |
| **CONFIGURABLE** | Works when correctly configured; may require external services |
| **REQUIRES EXTERNAL SERVICE** | Gateway depends on an external component you must supply |
| **DELEGATED TO DOWNSTREAM SERVICE** | Gateway forwards; enforcement is downstream |
| **NOT IMPLEMENTED** | Not present or intentionally omitted |

---

## Solution Structure (14 projects)

| Project | Role |
|---|---|
| `Gateway.Host` | ASP.NET Core host |
| `Gateway.Framework.Core` | Domain abstractions, errors, responses |
| `Gateway.Framework.Shared` | DI, HTTP helpers |
| `Gateway.Framework.Infrastructure` | Configuration options |
| `Gateway.Framework.Security` | JWT, authorization, secure headers |
| `Gateway.Framework.Logging` | Serilog, audit, masking |
| `Gateway.Framework.Monitoring` | Health checks, OpenTelemetry |
| `Gateway.Framework.Resilience` | Banking-safe HttpClient policies |
| `Gateway.Framework.Gateway` | YARP, middleware, rate limiting |
| `Gateway.Framework.Plugins` | **Plugin contract and manager** |
| `Gateway.Bank.Bank1` | Sample bank plugin (accounts/balance) |
| `Gateway.Bank.Bank2` | Sample bank plugin (payments/transfers) |
| `Gateway.Tests.Unit` | 18 unit tests |
| `Gateway.Tests.Integration` | 18 integration tests |

See [docs/architecture.md](docs/architecture.md) and [docs/plugin-development.md](docs/plugin-development.md).

---

## Architecture

```
Client → External OIDC IdP → JWT → Gateway.Host → Security → Plugin Manager → Bank Plugins → Bank APIs
```

The gateway is **stateless**. It does **not** issue tokens or maintain an auth database.

---

## Plugin Architecture — IMPLEMENTED

| Component | Status |
|---|---|
| `IBankingGatewayPlugin` | **IMPLEMENTED** |
| `IBankingGatewayPluginManager` | **IMPLEMENTED** |
| `BankingPluginCapability` | **IMPLEMENTED** |
| Plugin configuration (`Plugins:{Name}`) | **IMPLEMENTED** |
| Plugin YARP route merge | **IMPLEMENTED** |
| Plugin health checks | **IMPLEMENTED** |
| Sample Bank1 + Bank2 plugins | **IMPLEMENTED** |
| Framework bank-agnostic | **IMPLEMENTED** |

### Registering plugins (Host)

```csharp
builder.Services.AddBankingGatewayPlugins(builder.Configuration, plugins =>
{
    plugins.AddPlugin<Bank1Plugin>();
    plugins.AddPlugin<Bank2Plugin>();
});
```

### Plugin configuration

```json
"Plugins": {
  "Bank1": { "Enabled": true, "BaseUrl": "http://localhost:5201/", "TimeoutSeconds": 30 },
  "Bank2": { "Enabled": true, "BaseUrl": "http://localhost:5202/", "TimeoutSeconds": 30 }
}
```

### Plugin routes

| Plugin | Route | Capabilities |
|---|---|---|
| Bank1 | `/api/v1/banks/bank1/accounts/**` | Accounts, Balance |
| Bank2 | `/api/v1/banks/bank2/payments/**` | Payment, Transfer |

---

## Authentication — IMPLEMENTED

**JWT/OIDC validation only; external Identity Provider required.**

The gateway validates tokens from your IdP. It does **not** implement SSO or token issuance.

| Setting | Production |
|---|---|
| `Auth:Enabled` | Must be `true` |
| `Auth:Authority` | Required |
| `Auth:Audience` | Required |
| `Auth:AllowDevelopmentAnonymous` | Blocked outside Development |

---

## Idempotency — DELEGATED TO DOWNSTREAM SERVICE

The gateway **propagates** `Idempotency-Key` to downstream services. It does **not** enforce idempotency.

---

## Feature Matrix

| Feature | Status |
|---|---|
| YARP reverse proxy | **IMPLEMENTED** |
| Plugin YARP routes | **IMPLEMENTED** |
| JWT validation | **IMPLEMENTED** |
| Authorization (roles/scopes) | **IMPLEMENTED** |
| Rate limiting | **IMPLEMENTED** |
| Audit logging | **IMPLEMENTED** |
| Correlation IDs | **IMPLEMENTED** |
| Idempotency key propagation | **DELEGATED TO DOWNSTREAM SERVICE** |
| Banking-safe resilience | **IMPLEMENTED** |
| OpenTelemetry | **IMPLEMENTED** |
| Health checks + plugin health | **IMPLEMENTED** |
| CORS | **NOT IMPLEMENTED** (configure at ingress) |
| ICache / IEncryptionService | **NOT IMPLEMENTED** (not registered) |
| Token issuance / Identity Server | **NOT IMPLEMENTED** |

---

## Tests (36 total)

```bash
dotnet test GateWayFrameWork.sln
```

| Area | Coverage |
|---|---|
| Auth config validation | Unit |
| Sensitive data masking | Unit |
| Plugin manager (duplicate BankCode, config validation, routes) | Unit |
| Health endpoints | Integration |
| JWT pipeline (11 scenarios) | Integration |
| Plugin routes and health status | Integration |

---

## Build & Run

```bash
dotnet restore GateWayFrameWork.sln
dotnet build GateWayFrameWork.sln
dotnet test GateWayFrameWork.sln
dotnet run --project Gateway.Host/Gateway.Host.csproj
```

### Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /health/live` | Liveness |
| `GET /health/ready` | Readiness (includes plugin checks) |
| `GET /api/v1/health/status` | Gateway + plugin status JSON |
| `/api/v1/banks/bank1/accounts/**` | Bank1 plugin proxy |
| `/api/v1/banks/bank2/payments/**` | Bank2 plugin proxy |

---

## Production Environment Variables

| Variable | Required |
|---|---|
| `Auth__Authority` | Yes |
| `Auth__Audience` | Yes |
| `Plugins__Bank1__BaseUrl` | If Bank1 enabled |
| `Plugins__Bank2__BaseUrl` | If Bank2 enabled |
| `OpenTelemetry__OtlpEndpoint` | Optional |

---

## Docker & Kubernetes

```bash
docker build -t gateway-host:latest -f Gateway.Host/Dockerfile .
kubectl apply -f k8s/deployment.yaml
```

Docker runs as non-root. Kubernetes manifest includes `securityContext`, probes, and secret refs.

---

## External Dependencies

1. **OIDC Identity Provider** — token issuance and JWKS
2. **Downstream bank APIs** — per plugin BaseUrl
3. **OTLP collector** — optional
4. **Ingress/WAF** — TLS, CORS if needed

---

## License

TBD
