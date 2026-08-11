# Banking Gateway Architecture

## Overview

The Banking Gateway is a **stateless** .NET 8 API gateway that validates JWTs from an external OIDC provider, applies security controls, and routes traffic to downstream **bank services** through **bank plugins**.

Each bank integration has two parts:

1. **Plugin** (`Gateway.Bank.*`) — compiled into `Gateway.Host`; contributes YARP routes, health checks, and HttpClient configuration.
2. **Service** (`services/*.Service`) — optional independent ASP.NET Core app holding bank-specific business logic and API contracts.

```mermaid
flowchart TD
    Client[Client / Mobile / Server]
    IdP[External OIDC Identity Provider]
    Host[Gateway.Host :5000]
    Security[Security Layer]
    Plugins[Plugin Manager]
    Bank1Plugin[Gateway.Bank.Bank1]
    Bank2Plugin[Gateway.Bank.Bank2]
    Bank1Service[Bank1.Service :5101]
    Bank2Service[Bank2.Service :5102]
    RealAPI[Actual Bank APIs]

    Client --> IdP
    IdP -->|JWT| Client
    Client --> Host
    Host --> Security
    Security --> Plugins
    Plugins --> Bank1Plugin
    Plugins --> Bank2Plugin
    Bank1Plugin -->|HTTP| Bank1Service
    Bank2Plugin -->|HTTP| Bank2Service
    Bank1Service -.->|future| RealAPI
    Bank2Service -.->|future| RealAPI
```

## Request Flow (Example)

`GET /api/v1/banks/bank1/accounts` through the gateway:

1. Client sends request to `Gateway.Host` with optional JWT and `X-Correlation-Id`.
2. Gateway validates auth, rate limits, audit logs, adds/propagates correlation ID.
3. YARP matches plugin route `bank1` → cluster destination `Plugins:Bank1:BaseUrl`.
4. Path transform: remove `/api/v1/banks/bank1`, prefix `/api` → `/api/accounts`.
5. `Bank1.Service` handles the request and returns sample account data.

## Dependency Direction

```
Gateway.Host
    ↓
Gateway.Bank.* (plugins)
    ↓
Gateway.Framework.Plugins
    ↓
Gateway.Framework.* (Core, Security, Logging, Gateway, …)

services/Bank*.Service  (independent — no reference to Gateway.Host)
```

**Critical rules:**

- The framework never references bank-specific projects.
- Bank **plugins** depend on `Gateway.Framework.Plugins` only.
- Bank **services** are standalone; plugins reach them via configured `BaseUrl`.
- Do **not** duplicate gateway JWT/security inside bank services.

## Gateway Responsibilities

| Concern | Owner |
|---|---|
| JWT validation | Gateway |
| Authorization (roles/scopes) | Gateway |
| Rate limiting | Gateway |
| Correlation IDs | Gateway (propagated downstream) |
| Audit logging | Gateway |
| YARP routing | Gateway + plugins |
| Global resilience policies | Gateway |
| OpenTelemetry (gateway) | Gateway |
| Idempotency-Key propagation | Gateway (enforcement downstream) |

## Bank Service Responsibilities

| Concern | Owner |
|---|---|
| Bank API contracts (controllers) | Bank service |
| Business logic | Bank service |
| Request/response mapping | Bank service |
| Bank-specific error handling | Bank service |
| Bank-specific auth/signing to real APIs | Bank service |
| Service Swagger / OpenAPI | Bank service |
| Service health (`/health/live`, `/health/ready`) | Bank service |
| Demo idempotency (Bank2) | Bank service |

## Framework Modules

| Module | Responsibility |
|---|---|
| `Gateway.Framework.Core` | Domain abstractions, errors, API responses, idempotency constants |
| `Gateway.Framework.Shared` | DI helpers, HTTP extensions, JSON defaults |
| `Gateway.Framework.Infrastructure` | Configuration options |
| `Gateway.Framework.Security` | JWT validation, authorization, secure headers |
| `Gateway.Framework.Logging` | Serilog, audit logging, sensitive data masking |
| `Gateway.Framework.Monitoring` | Health checks, OpenTelemetry |
| `Gateway.Framework.Resilience` | Banking-safe HttpClient retry/circuit breaker |
| `Gateway.Framework.Gateway` | YARP, middleware, rate limiting, API versioning |
| `Gateway.Framework.Plugins` | Plugin contract, manager, YARP route merge |

## Plugin Architecture

**IMPLEMENTED**

- `IBankingGatewayPlugin` — stable bank integration contract
- `IBankingGatewayPluginManager` — registration, validation, lifecycle, status
- `BankingPluginCapability` — declared capabilities per bank
- YARP routes/clusters contributed via declarative `PluginRouteBuilder`
- Plugin health checks call `{BaseUrl}health/live`
- Correlation ID propagation via `AddBankPluginHttpClient`

### Sample route mapping

| Gateway path | Transformed service path | Service |
|---|---|---|
| `/api/v1/banks/bank1/accounts` | `/api/accounts` | Bank1.Service |
| `/api/v1/banks/bank2/payments` | `/api/payments` | Bank2.Service |
| `/api/v1/banks/bank2/transfers` | `/api/transfers` | Bank2.Service |

## Development Topology

| Process | URL | Swagger |
|---|---|---|
| Gateway.Host | http://localhost:5000 | — |
| Bank1.Service | http://localhost:5101 | http://localhost:5101/swagger |
| Bank2.Service | http://localhost:5102 | http://localhost:5102/swagger |

Docker Compose runs the same three services with internal DNS (`bank1-service`, `bank2-service`, `gateway`).

## Production Fail-Fast

Production startup fails when:

- Auth disabled or missing Authority/Audience
- Development anonymous bypass enabled
- Localhost downstream URLs (static YARP or enabled plugins)
- Enabled plugin missing BaseUrl

## Production Networking

Bank services run on **internal** cluster addresses only. The gateway `Plugins:{Bank}:BaseUrl` must point to internal service URLs. Public ingress terminates at the gateway — never at bank services.

## Stateless Design

No authentication database. No token issuance. No gateway idempotency store unless explicitly added with a documented correctness model.
