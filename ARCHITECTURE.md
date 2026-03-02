# ClaimsOps Architecture Documentation

**Version:** 1.0.0  
**Last Updated:** March 2, 2026

## Table of Contents

1. [System Overview](#system-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture Decisions](#architecture-decisions)
4. [Request Lifecycle](#request-lifecycle)
5. [Data Flow](#data-flow)
6. [Infrastructure](#infrastructure)
7. [Security](#security)
8. [Development Guide](#development-guide)
9. [Operations](#operations)

---

## System Overview

ClaimsOps is a microservices-based insurance claims management system demonstrating modern enterprise patterns.

### Components

```text
┌─────────────────────────────────────────────────────────────┐
│                        Client Layer                          │
│  (Browser, Mobile App, API Consumer)                        │
└──────────────────┬──────────────────────────────────────────┘
                   │ HTTP/JSON
                   ▼
┌─────────────────────────────────────────────────────────────┐
│                   Claims Service (C# .NET)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐  │
│  │ Controllers  │──│   Services   │──│  Repositories   │  │
│  │  (HTTP API)  │  │  (Business)  │  │  (Data Access)  │  │
│  └──────────────┘  └──────────────┘  └────────┬────────┘  │
│                                                 │            │
└─────────────────────────────────────────────────┼──────────┘
                   │                              │
                   │ HTTP (Best-effort)          │ EF Core
                   ▼                              ▼
         ┌──────────────────┐         ┌──────────────────┐
         │  Audit Service   │         │   PostgreSQL     │
         │ (Python FastAPI) │         │   (Database)     │
         │  - In-memory     │         │  - Claims table  │
         │  - Events log    │         │  - Migrations    │
         └──────────────────┘         └──────────────────┘
```

### Key Characteristics

- **Microservices:** Two independent services with different tech stacks
- **RESTful API:** Standard HTTP/JSON communication
- **Polyglot:** Demonstrates .NET + Python integration
- **Best-effort Audit:** Non-blocking event recording
- **Container-native:** All services run in Docker

---

## Technology Stack

### Backend Services

#### Claims Service (C# .NET 10.0)

**Purpose:** Primary API for claims operations (create, read, update, delete)

**Why .NET?**
- **Type safety:** Strong typing reduces runtime errors in production
- **Performance:** Optimized for high-throughput server workloads
- **Enterprise maturity:** Proven in regulated industries (finance, healthcare, insurance)
- **Rich ecosystem:** Authentication, logging, ORM, testing frameworks
- **Compile-time validation:** Many bugs caught before deployment

**Key Technologies:**
- ASP.NET Core 10.0 - Web framework
- Entity Framework Core 10.0 - ORM with migrations
- Npgsql - PostgreSQL driver
- Port: 5115

#### Audit Service (Python 3.11 + FastAPI)

**Purpose:** Lightweight event recording for operational traceability

**Why Python + FastAPI?**
- **Rapid development:** MVP built in under 1 hour
- **Minimal boilerplate:** Less code compared to Java/C# for simple services
- **Async support:** ASGI-based for high concurrency
- **Auto documentation:** OpenAPI/Swagger generated automatically
- **Polyglot demonstration:** Shows service independence

**Key Technologies:**
- FastAPI - Modern async web framework
- Pydantic - Data validation
- Uvicorn - ASGI server
- Port: 8000

### Data Layer

#### PostgreSQL 15

**Why PostgreSQL?**
- **ACID compliance:** Required for financial transaction integrity
- **JSON support:** Hybrid relational + document flexibility
- **Open source:** No licensing costs
- **Scalability:** Handles millions of rows efficiently
- **Community:** Large ecosystem of tools and extensions

**Schema Management:**
- Entity Framework Core migrations (code-first)
- Version-controlled schema changes
- Automatic application on startup

### Infrastructure

#### Docker Compose

**Purpose:** Multi-container orchestration for local development

**Benefits:**
- **Reproducibility:** Identical environments across all developers
- **Service dependencies:** Automatic startup ordering with health checks
- **Isolation:** Each service has independent dependencies
- **Easy reset:** `docker compose down -v` for clean state

**Network:**
- Bridge network `claimsops-network`
- Service-to-service DNS resolution
- Internal communication (postgres, claims-service, audit-service)

---

## Architecture Decisions

### Layered Architecture (Claims Service)

```text
Controllers/     → HTTP boundary (routing, validation)
    ↓
Services/        → Business logic (orchestration, rules)
    ↓
Repositories/    → Data access (ORM abstraction)
    ↓
EF Core          → Database driver
    ↓
PostgreSQL       → Persistent storage
```

**Why this pattern?**
- **Separation of concerns:** Each layer has single responsibility
- **Testability:** Can mock dependencies for unit tests
- **Maintainability:** Changes isolated to specific layers
- **Standard practice:** Well-understood pattern in enterprise

### DTOs vs Domain Models

**DTOs (Data Transfer Objects):**
- `CreateClaimRequest` - API input contract
- `ClaimResponse` - API output contract

**Domain Model:**
- `Claim` - Internal entity representation

**Why separate?**
- **API stability:** Can change internal model without breaking clients
- **Validation boundary:** DTOs enforce external constraints
- **Security:** Don't expose internal fields (audit trails, soft deletes)
- **Flexibility:** Multiple DTOs can map to single domain model

### Best-Effort Audit Pattern

```csharp
// Claims service doesn't fail if audit fails
try {
    await _httpClient.PostAsync("/audit", content);
} catch (Exception ex) {
    _logger.LogWarning("Audit failed: {Message}", ex.Message);
    // Continue anyway - claim was saved successfully
}
```

**Why this approach?**
- **Resilience:** Primary operation succeeds even if audit service is down
- **Non-critical path:** Audit is observability, not business requirement
- **Graceful degradation:** System remains operational during partial failures

**Trade-offs:**
- **Eventual consistency:** Audit events may be delayed or lost
- **No guaranteed delivery:** For critical audit, use message queue (Kafka, RabbitMQ)

---

## Request Lifecycle

### POST /api/claims Flow

Complete walkthrough of creating a new claim:

```text
Client
  │
  │ 1. POST /api/claims { memberId, amount, currency }
  ▼
┌─────────────────────────────────────────────┐
│ ClaimsController.CreateClaim                │
│ - Receives JSON (ASP.NET model binding)     │
│ - Validates DTO (data annotations)          │
│ - Delegates to service layer                │
└──────────────────┬──────────────────────────┘
                   │ 2. CreateClaimAsync(request)
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimService.CreateClaimAsync               │
│ - Maps DTO → Domain entity                  │
│ - Assigns defaults:                         │
│   • Id = Guid.NewGuid()                     │
│   • Status = Draft                          │
│   • CreatedAt = DateTime.UtcNow             │
│ - Calls repository                          │
└──────────────────┬──────────────────────────┘
                   │ 3. CreateAsync(claim)
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimRepository.CreateAsync                 │
│ - DbContext.Claims.Add(claim)               │
│ - SaveChangesAsync()                        │
│ - Returns persisted entity with ID          │
└──────────────────┬──────────────────────────┘
                   │ 4. INSERT INTO claims (...)
                   ▼
             ┌──────────┐
             │PostgreSQL│ ← Source of truth
             └──────────┘
                   │
                   │ 5. Best-effort audit
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimService.RecordAuditEventAsync          │
│ - HTTP POST to audit-service:8000/audit     │
│ - Non-blocking (try-catch)                  │
│ - Logs warning on failure                   │
└──────────────────┬──────────────────────────┘
                   │ 6. POST /audit { claimId, action }
                   ▼
             ┌────────────┐
             │Audit Service│ ← In-memory (MVP)
             └────────────┘
                   │
                   │ 7. 201 Created response
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimsController returns                    │
│ - Status: 201 Created                       │
│ - Location: /api/claims/{id}                │
│ - Body: ClaimResponse DTO                   │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
                 Client
```

### Data Transformation

| Stage | Format | Example | Purpose |
| --- | --- | --- | --- |
| **1. Request** | JSON | `{"memberId":"MBR-001","amount":100.0}` | API contract |
| **2. DTO** | C# object | `CreateClaimRequest` | Validation boundary |
| **3. Domain** | Entity | `Claim` (with Id, Status, CreatedAt) | Business logic |
| **4. Persistence** | SQL row | `INSERT INTO claims (...)` | Durable storage |
| **5. Response** | DTO | `ClaimResponse` | API contract |

### Field Mapping Rules

| Field | Source | Default | Validation |
| --- | --- | --- | --- |
| `Id` | Service | `Guid.NewGuid()` | Always system-generated |
| `MemberId` | Request | None | Required, 1-50 chars |
| `Amount` | Request | None | Required, > 0 |
| `Currency` | Request | "USD" | Optional, 3-char ISO |
| `Status` | Service | `Draft` | Enum: Draft, Submitted, Approved, Rejected |
| `CreatedAt` | Service | `DateTime.UtcNow` | Always UTC |

---

## Data Flow

### Database Schema

```sql
CREATE TABLE claims (
    id            UUID PRIMARY KEY,
    member_id     VARCHAR(50) NOT NULL,
    amount        DECIMAL(18,2) NOT NULL,
    currency      VARCHAR(3) NOT NULL DEFAULT 'USD',
    status        VARCHAR(20) NOT NULL,
    created_at    TIMESTAMP NOT NULL
);
```

### Entity Framework Mapping

```csharp
public class Claim
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string MemberId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Migrations

**Creating migration:**
```bash
cd services/claims-service
dotnet ef migrations add DescriptiveName
```

**Applying migration:**
```bash
# Manual
dotnet ef database update

# Automatic (on container startup)
# Program.cs contains: dbContext.Database.Migrate()
```

**Migration files location:**
- `services/claims-service/Migrations/`
- Version-controlled (part of git history)
- Applied automatically when container starts

---

## Infrastructure

### Docker Compose Configuration

**Services:**

| Service | Image | Port | Depends | Purpose |
| --- | --- | --- | --- | --- |
| postgres | postgres:15 | 5432 | - | Database |
| claims-service | Custom (.NET) | 5115 | postgres | Primary API |
| audit-service | Custom (Python) | 8000 | - | Event recording |

**Network:**
- Type: Bridge
- Name: `claimsops-network`
- DNS: Automatic service resolution (e.g., `http://postgres:5432`)

**Volumes:**
- `postgres_data` → `/var/lib/postgresql/data` (persistent storage)

**Health checks:**
```yaml
postgres:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U claimsops_user"]
    interval: 10s
    timeout: 5s
    retries: 5
```

### Environment Variables

**Required variables** (set in `docker/.env`):
```bash
# Database
POSTGRES_USER=claimsops_user
POSTGRES_PASSWORD=secure_password_here
POSTGRES_DB=claimsops_db

# Connection strings (auto-built from above)
CONNECTION_STRING=Host=postgres;Database=claimsops_db;Username=claimsops_user;Password=secure_password_here

# Service URLs
AUDIT_SERVICE_URL=http://audit-service:8000
```

**Security:** Never commit `docker/.env` file (listed in `.gitignore`)

### Service Communication

**Claims Service → PostgreSQL:**
- Protocol: PostgreSQL wire protocol
- Connection: `Host=postgres;Port=5432;...`
- Driver: Npgsql via Entity Framework Core

**Claims Service → Audit Service:**
- Protocol: HTTP/JSON
- URL: `http://audit-service:8000/audit`
- Method: POST (non-blocking)

---

## Security

### Secrets Management

#### Local Development

**File structure:**
```
docker/
├── .env              ← Contains secrets (NEVER COMMIT)
├── .env.example      ← Template (SAFE TO COMMIT)
└── docker-compose.yml
```

**Generate secure password:**
```bash
openssl rand -base64 24
# Copy output to docker/.env
```

**Verification:**
```bash
# Check .env is ignored
grep "\.env" .gitignore

# Verify not staged
git status | grep "\.env"  # Should be empty
```

#### Production

**Use proper secrets managers:**

| Platform | Service | Pattern |
| --- | --- | --- |
| AWS | Secrets Manager | `arn:aws:secretsmanager:region:...` |
| Azure | Key Vault | `https://vault.azure.net/secrets/...` |
| GCP | Secret Manager | `projects/{id}/secrets/{name}` |
| Kubernetes | Secrets | ConfigMap + Secret resources |

**Never:**
- ❌ Hardcode credentials in code
- ❌ Commit `.env` files
- ❌ Log sensitive data
- ❌ Expose secrets in error messages

### Input Validation

**Validation layers:**

1. **DTO Data Annotations** (automatic):
```csharp
public class CreateClaimRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string MemberId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}
```

2. **Model State** (controller):
```csharp
if (!ModelState.IsValid)
    return BadRequest(ModelState);
```

3. **Business Rules** (service layer):
```csharp
if (claim.Amount > MAX_CLAIM_AMOUNT)
    throw new BusinessException("Amount exceeds limit");
```

### CORS Configuration

**Current** (`appsettings.json`):
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001"
    ]
  }
}
```

**Production:** Update with actual frontend domains

**GitHub Codespaces:** Requires dynamic origin configuration (see README)

### Authentication & Authorization

**Current status:** Not implemented (Phase 1 MVP)

**Roadmap (Phase 2):**
- JWT token-based authentication
- Role-based access control (RBAC)
- Service-to-service API keys
- OAuth2 / OpenID Connect integration

---

## Development Guide

### Prerequisites

- .NET 10.0 SDK
- Docker Desktop
- Python 3.11+ (for local audit service development)
- Git

### Initial Setup

```bash
# Clone repository
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app

# Configure environment
cd docker
cp .env.example .env
# Edit .env with secure passwords

# Start services
docker compose up -d --build

# Verify
docker compose ps
curl http://localhost:5115/health
curl http://localhost:8000/health
```

### Local Development (Without Docker)

**Terminal 1 - PostgreSQL:**
```bash
docker compose up -d postgres
```

**Terminal 2 - Claims Service:**
```bash
cd services/claims-service

# Update connection string (use localhost instead of postgres)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=claimsops_db;..."

# Run
dotnet run

# API available at http://localhost:5115
```

**Terminal 3 - Audit Service:**
```bash
cd services/audit-service

python3 -m venv venv
source venv/bin/activate  # Linux/Mac
# venv\Scripts\activate   # Windows

pip install -r requirements.txt
uvicorn main:app --reload --port 8000

# API available at http://localhost:8000
```

### Creating Migrations

```bash
cd services/claims-service

# Add migration
dotnet ef migrations add YourMigrationName

# Apply locally
dotnet ef database update

# Deploy: migrations auto-apply on container startup
```

### Adding New Endpoints

1. **Define DTO** (`DTOs/YourRequestDto.cs`)
2. **Add Repository Method** (`Repositories/IYourRepository.cs`)
3. **Implement Repository** (`Repositories/YourRepository.cs`)
4. **Add Service Method** (`Services/IYourService.cs`)
5. **Implement Service** (`Services/YourService.cs`)
6. **Add Controller Action** (`Controllers/YourController.cs`)
7. **Register DI** (if new interfaces in `Program.cs`)
8. **Test locally** with `dotnet run`

### Testing

**Manual tests:**
```bash
# Health checks
curl http://localhost:5115/health
curl http://localhost:8000/health

# Create claim
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"MBR-TEST","amount":100.0,"currency":"USD"}'

# List claims
curl http://localhost:5115/api/claims

# View audit events
curl http://localhost:8000/audit
```

**Automated tests** (future):
- xUnit for C# unit tests
- pytest for Python tests
- Integration tests with test containers

---

## Operations

### Common Commands

**Docker Compose:**
```bash
# Start all services
docker compose -f docker/docker-compose.yml up -d

# View logs (all services)
docker compose -f docker/docker-compose.yml logs -f

# View logs (specific service)
docker compose -f docker/docker-compose.yml logs -f claims-service

# Check status
docker compose -f docker/docker-compose.yml ps

# Restart service
docker compose -f docker/docker-compose.yml restart claims-service

# Stop all (keep data)
docker compose -f docker/docker-compose.yml stop

# Remove all (keep volumes)
docker compose -f docker/docker-compose.yml down

# Full reset (delete data)
docker compose -f docker/docker-compose.yml down -v
```

**Database Access:**
```bash
# Connect to PostgreSQL
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db

# SQL queries
SELECT * FROM claims;
\dt   -- List tables
\q    -- Quit
```

**.NET Commands:**
```bash
cd services/claims-service

dotnet build                    # Compile
dotnet run                      # Run locally
dotnet test                     # Run tests
dotnet add package PackageName  # Install NuGet package
```

### Troubleshooting

**Port already in use:**
```bash
# Find process
lsof -i :5115
kill -9 <PID>

# Or change port in docker-compose.yml
ports:
  - "5116:5115"  # External:Internal
```

**Database connection failed:**
```bash
# Check PostgreSQL is running
docker compose ps postgres

# Check health
docker compose logs postgres

# Reset database
docker compose down -v postgres
docker compose up -d postgres
sleep 10
docker compose up -d claims-service
```

**Migrations not applied:**
```bash
# Check logs
docker compose logs claims-service | grep migration

# Manual application
cd services/claims-service
dotnet ef database update
```

**Claims service can't reach audit service:**
```bash
# Verify network
docker network ls | grep claimsops

# Check service DNS
docker exec -it claimsops-claims-service ping audit-service

# Check environment variable
docker exec -it claimsops-claims-service printenv AUDIT_SERVICE_URL
```

**Clean slate reset:**
```bash
# Nuclear option - removes everything
docker compose down -v
docker system prune -a --volumes
docker compose up -d --build
```

### Monitoring (Future)

**Phase 4 roadmap:**
- Prometheus for metrics collection
- Grafana for visualization dashboards
- OpenTelemetry for distributed tracing
- Structured logging with Serilog/ELK stack

---

## Additional Resources

- [README.md](README.md) - Quick start guide
- [CONTRIBUTING.md](CONTRIBUTING.md) - Development guidelines
- [GitHub Issues](https://github.com/SvillarroelZ/claimsops-app/issues) - Bug reports and features

---

**Document maintained by:** Engineering Team  
**Questions?** Open an issue on GitHub
