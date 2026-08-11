# GateWay Framework — .NET 8 Banking API Gateway

A modular **banking API gateway** on **.NET 8** and **YARP** with a **plugin architecture** for bank integrations. Each bank can also run as an **independent ASP.NET Core service** while the gateway routes traffic through bank plugins.

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

## Solution Structure (27+ projects)

| Project | Role |
|---|---|
| `Gateway.Host` | ASP.NET Core gateway host (port **5000**) |
| `Gateway.Framework.*` | Core framework modules (security, logging, YARP, plugins, …) |
| `Gateway.Bank.Bank1` | Bank1 **plugin** — gateway routing/config only |
| `Gateway.Bank.Bank2` | Bank2 **plugin** — gateway routing/config only |
| `services/Bank1.Service` | Bank1 **API host** (port **5101**) |
| `services/Bank1.Service.{Domain,Application,Contracts,Infrastructure}` | Bank1 Clean Architecture layers |
| `services/Bank2.Service` | Bank2 **API host** (port **5102**) |
| `services/Bank2.Service.{Domain,Application,Contracts,Infrastructure}` | Bank2 Clean Architecture layers |
| `services/Shared/Banking.Service.Audit.Abstractions` | Shared audit contract (technical only) |
| `*.Service.Tests` | Unit + integration + architecture tests for each bank service |
| `Gateway.Tests.Unit` | Gateway unit tests |
| `Gateway.Tests.Integration` | Gateway integration tests (auth, plugins, gateway→bank E2E) |

See [docs/architecture.md](docs/architecture.md), [docs/bank-service-development.md](docs/bank-service-development.md), and [docs/plugin-development.md](docs/plugin-development.md).

---

## Architecture

```
Client
  ↓
Gateway.Host (:5000)
  ↓
Gateway Framework (JWT, rate limit, audit, YARP, resilience, OTel)
  ↓
Bank Plugin (Gateway.Bank.Bank1 / Bank2)
  ↓
Bank Service (Bank1.Service :5101 / Bank2.Service :5102)
  ↓
(Future) Actual Bank API
```

### Plugin vs Service

| | **Plugin** (`Gateway.Bank.*`) | **Service** (`services/*.Service`) |
|---|---|---|
| Purpose | Gateway integration: YARP routes, health checks, HttpClient config | Bank-specific API, business logic, mapping |
| Runs in | `Gateway.Host` process | Independent ASP.NET Core app |
| Security | Uses gateway JWT/auth — does not duplicate it | Bank-specific signing/auth to real bank APIs (demo only here) |
| Configuration | `Plugins:Bank1:BaseUrl` points to service URL | Own `appsettings.json`, Swagger, health |

The gateway is **stateless**. It does **not** issue tokens or maintain an auth database.

---

## Local Development — Run All Three Apps

Open **three terminals** (or use Docker Compose below):

### Terminal 1 — Bank1 Service

```bash
dotnet run --project services/Bank1.Service/Bank1.Service.csproj
```

| URL | Purpose |
|---|---|
| http://localhost:5101/swagger | Bank1 Swagger UI |
| http://localhost:5101/api/accounts | List sample accounts |
| http://localhost:5101/health/live | Liveness |
| http://localhost:5101/health/ready | Readiness |

### Terminal 2 — Bank2 Service

```bash
dotnet run --project services/Bank2.Service/Bank2.Service.csproj
```

| URL | Purpose |
|---|---|
| http://localhost:5102/swagger | Bank2 Swagger UI |
| http://localhost:5102/api/payments | List sample payments |
| http://localhost:5102/api/transfers | Create sample transfer (POST) |
| http://localhost:5102/health/live | Liveness |
| http://localhost:5102/health/ready | Readiness |

### Terminal 3 — Gateway

```bash
dotnet run --project Gateway.Host/Gateway.Host.csproj
```

| URL | Purpose |
|---|---|
| http://localhost:5000/health/live | Gateway liveness |
| http://localhost:5000/health/ready | Readiness (includes plugin checks) |
| http://localhost:5000/api/v1/health/status | Gateway + plugin status JSON |
| http://localhost:5000/api/v1/banks/bank1/accounts | Proxied → Bank1 service |
| http://localhost:5000/api/v1/banks/bank2/payments | Proxied → Bank2 service |

### Plugin configuration (`Gateway.Host/appsettings.json`)

```json
"Plugins": {
  "Bank1": { "Enabled": true, "BaseUrl": "http://localhost:5101/", "TimeoutSeconds": 30 },
  "Bank2": { "Enabled": true, "BaseUrl": "http://localhost:5102/", "TimeoutSeconds": 30 }
}
```

---

## Testing Checklist

| # | Test | How |
|---|---|---|
| 1 | Direct Bank1 Swagger | Open http://localhost:5101/swagger, call `GET /api/accounts` |
| 2 | Direct Bank2 Swagger | Open http://localhost:5102/swagger, call `GET /api/payments` |
| 3 | Gateway → Bank1 | `GET http://localhost:5000/api/v1/banks/bank1/accounts` |
| 4 | Gateway → Bank2 | `GET http://localhost:5000/api/v1/banks/bank2/payments` |
| 5 | Authentication | Enable `Auth:Enabled=true` + valid JWT; without token → 401 |
| 6 | Authorization | JWT missing required scope/role → 403 |
| 7 | Correlation ID | Send `X-Correlation-Id: my-id` through gateway; echoed in response |
| 8 | Idempotency-Key | Send `Idempotency-Key` on POST to Bank2 via gateway; propagated to service |
| 9 | Downstream failure | Stop Bank1 service; gateway plugin health shows degraded; proxy returns 502 |
| 10 | Plugin health | `GET http://localhost:5000/api/v1/health/status` lists BANK1/BANK2 |

Automated coverage: `dotnet test GateWayFrameWork.sln` (**64 tests**).

---

## Docker Compose (Development)

```bash
docker compose up --build
```

| Service | Host URL |
|---|---|
| gateway | http://localhost:5000 |
| bank1-service | http://localhost:5101 |
| bank2-service | http://localhost:5102 |
| bank1-db (PostgreSQL) | localhost:5433 |
| bank2-db (PostgreSQL) | localhost:5435 |
| bank1-log-db | localhost:5434 |
| bank2-log-db | localhost:5436 |

Each bank service connects only to its own business and audit databases. Docker sets `Database:Provider=Npgsql` with internal service hostnames.

Gateway environment variables wire plugins to internal service names (`http://bank1-service:8080/`).

> **Production:** Bank services must **not** be exposed publicly. Only the gateway ingress should be reachable; bank services run on internal cluster networking.

---

## Build & Test

```bash
dotnet restore GateWayFrameWork.sln
dotnet build GateWayFrameWork.sln
dotnet test GateWayFrameWork.sln
dotnet sln list
```

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

The gateway **propagates** `Idempotency-Key` to downstream services. Bank2.Service demonstrates local idempotency handling; the gateway does not maintain an idempotency store.

---

## Feature Matrix

| Feature | Status |
|---|---|
| YARP reverse proxy | **IMPLEMENTED** |
| Plugin YARP routes | **IMPLEMENTED** |
| Independent bank services | **IMPLEMENTED** |
| JWT validation | **IMPLEMENTED** |
| Authorization (roles/scopes) | **IMPLEMENTED** |
| Rate limiting | **IMPLEMENTED** |
| Audit logging | **IMPLEMENTED** |
| Correlation IDs | **IMPLEMENTED** |
| Idempotency key propagation | **DELEGATED TO DOWNSTREAM SERVICE** |
| Banking-safe resilience | **IMPLEMENTED** |
| OpenTelemetry | **IMPLEMENTED** |
| Health checks + plugin health | **IMPLEMENTED** |
| Token issuance / Identity Server | **NOT IMPLEMENTED** |

---

## Production Environment Variables

| Variable | Required |
|---|---|
| `Auth__Authority` | Yes |
| `Auth__Audience` | Yes |
| `Plugins__Bank1__BaseUrl` | If Bank1 enabled (internal URL, not public) |
| `Plugins__Bank2__BaseUrl` | If Bank2 enabled (internal URL, not public) |
| `OpenTelemetry__OtlpEndpoint` | Optional |

---

## External Dependencies

1. **OIDC Identity Provider** — token issuance and JWKS
2. **Bank services** — per plugin `BaseUrl` (internal in production)
3. **OTLP collector** — optional
4. **Ingress/WAF** — TLS, CORS if needed

---

## License

TBD
