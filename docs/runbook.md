# ClaimsOps Application - Technical Runbook

**Version:** 1.0.0  
**Last Updated:** March 2, 2026  
**Environment:** Development (MVP)

---

## Table of Contents

1. [Technology Stack Overview](#technology-stack-overview)
2. [Project Architecture](#project-architecture)
3. [Build Order & Construction Rationale](#build-order--construction-rationale)
4. [Prerequisites](#prerequisites)
5. [Local Setup & Installation](#local-setup--installation)
6. [Running the Application](#running-the-application)
7. [Smoke Tests & Verification](#smoke-tests--verification)
8. [Common Commands Reference](#common-commands-reference)
9. [Troubleshooting](#troubleshooting)
10. [Development Workflow](#development-workflow)

---

## Technology Stack Overview

This section explains **why** each technology was chosen and **how** it fits into the overall architecture.

### Backend Services

#### 1. **Claims Service** (C# / .NET 10)

**What it is:**  
A RESTful Web API built with ASP.NET Core 10.0 that manages insurance claims lifecycle.

**Why we use it:**
- **Type Safety:** C# provides strong typing and compile-time checking, reducing runtime errors in enterprise systems.
- **Performance:** .NET is highly optimized for high-throughput server applications.
- **Enterprise Maturity:** ASP.NET Core is battle-tested in regulated industries (banking, insurance, healthcare).
- **Rich Ecosystem:** Extensive libraries for authentication, logging, data access (Entity Framework), and more.

**Key Components:**
- **Controllers:** Handle HTTP requests/responses (routing, validation)
- **Services:** Business logic layer (orchestration, validation, mapping)
- **Repositories:** Data access layer (abstracts database operations)
- **DTOs:** Data Transfer Objects separate API contracts from internal models

**Technologies Used:**
- **ASP.NET Core Web API:** Framework for building HTTP APIs
- **Entity Framework Core:** Object-Relational Mapper (ORM) for database access
- **Npgsql:** PostgreSQL driver for .NET

#### 2. **Audit Service** (Python / FastAPI)

**What it is:**  
A lightweight microservice for recording audit events from the claims service.

**Why we use it:**
- **Rapid Development:** Python + FastAPI enables extremely fast prototyping (MVP built in < 1 hour).
- **Simplicity:** Minimal boilerplate code compared to Java/C# for simple CRUD operations.
- **Async Support:** FastAPI is built on asynchronous Python (ASGI) for high concurrency.
- **Auto Documentation:** Automatically generates OpenAPI/Swagger docs from code annotations.
- **Polyglot Architecture:** Demonstrates service independence—different services can use different tech stacks.

**Key Components:**
- **Pydantic Models:** Type validation using Python type hints
- **In-Memory Storage:** Simple list for MVP (no database overhead)
- **Uvicorn:** ASGI server for running async Python applications

**Technologies Used:**
- **FastAPI:** Modern Python web framework
- **Pydantic:** Data validation library
- **Uvicorn:** Production ASGI server

### Data Layer

#### 3. **PostgreSQL 15**

**What it is:**  
Open-source relational database system.

**Why we use it:**
- **ACID Compliance:** Ensures data consistency for financial/insurance transactions.
- **JSON Support:** Hybrid relational + document store (flexible for evolving schemas).
- **Scalability:** Handles millions of rows efficiently.
- **Free & Open Source:** No licensing costs (important for enterprises managing budgets).

**Use Cases in This Project:**
- Claims data (structured tables)
- Supports EF Core migrations for schema versioning

### Infrastructure & Tooling

#### 4. **Docker Compose**

**What it is:**  
Tool for defining and running multi-container Docker applications using YAML configuration.

**Why we use it:**
- **Reproducibility:** Every developer gets identical environments (no "works on my machine" issues).
- **Service Orchestration:** Manages dependencies (e.g., claims-service waits for PostgreSQL health check).
- **Isolation:** Each service runs in its own container with isolated dependencies.
- **Easy Teardown:** `docker-compose down -v` removes all data for clean resets.

**Services Defined:**
1. **postgres:** Database container
2. **claims-service:** .NET API container
3. **audit-service:** Python FastAPI container

#### 5. **Entity Framework Core**

**What it is:**  
Object-Relational Mapper (ORM) for .NET that maps C# classes to database tables.

**Why we use it:**
- **Code-First Development:** Define models in C#, generate database schema automatically.
- **Migrations:** Version-controlled database schema changes (track history like Git for SQL).
- **Query Abstraction:** Write C# LINQ queries instead of raw SQL (safer, more maintainable).
- **Cross-Database:** Can switch from PostgreSQL to SQL Server with minimal code changes.

**Example Workflow:**
```csharp
// 1. Define model
public class Claim { public Guid Id { get; set; } ... }

// 2. Create migration
dotnet ef migrations add InitialCreate

// 3. Apply to database
dotnet ef database update
```

#### 6. **NuGet**

**What it is:**  
Package manager for .NET (similar to npm for Node.js, pip for Python).

**Why we use it:**
- **Dependency Management:** Install, update, and remove libraries with simple commands.
- **Version Control:** Lock specific versions to ensure consistent builds.
- **Centralized Repository:** Access thousands of vetted packages (Microsoft.EntityFrameworkCore, Npgsql, etc.).

**Common Commands:**
```bash
# Install package
dotnet add package EntityFrameworkCore

# Restore all packages (reads from .csproj)
dotnet restore

# List installed packages
dotnet list package
```

**In This Project:**
- `Microsoft.EntityFrameworkCore` - ORM core library
- `Microsoft.EntityFrameworkCore.Design` - Migration tools
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider

---

## Project Architecture

```
claimsops-app/
├── services/
│   ├── claims-service/          # C# .NET Web API
│   │   ├── Controllers/         # HTTP request handlers
│   │   ├── Services/            # Business logic
│   │   ├── Repositories/        # Data access
│   │   ├── Models/              # Domain entities
│   │   ├── DTOs/                # API request/response contracts
│   │   ├── Data/                # EF Core DbContext
│   │   ├── Migrations/          # Database schema versions
│   │   ├── Program.cs           # App entry point & DI config
│   │   ├── appsettings.json     # Configuration
│   │   └── Dockerfile           # Container definition
│   │
│   └── audit-service/           # Python FastAPI
│       ├── main.py              # App entry point & all endpoints
│       ├── requirements.txt     # Python dependencies
│       └── Dockerfile           # Container definition
│
├── docker/
│   ├── docker-compose.yml       # Multi-service orchestration
│   └── init.sql                 # PostgreSQL initialization
│
├── docs/
│   ├── runbook.md               # This file
│   └── security.md              # Security documentation
│
└── .env.example                 # Environment variable template
```

**Design Principles:**
1. **Separation of Concerns:** Each service has distinct responsibility (claims vs audit)
2. **Layered Architecture:** Controller → Service → Repository (clean boundaries)
3. **Dependency Injection:** All dependencies injected via constructor (testable, flexible)
4. **API-First:** DTOs decouple internal models from external contracts

---

## Build Order & Construction Rationale

This section explains **the order** in which components were built and **why** that sequence matters.

### Phase 1: Foundation (Claims Service Structure)

**What Was Built:**
- Basic .NET project structure (`ClaimsService.csproj`)
- Domain models (`Claim`, `ClaimStatus`)
- DTOs (`ClaimResponse`, `CreateClaimRequest`)
- Repository interface (`IClaimRepository`)
- Service interface (`IClaimService`)
- Controllers (`ClaimsController`, `HealthController`)

**Why This Order:**
1. **Define the Domain First:** Understand the business entities before writing code.
2. **Contracts Before Implementation:** Define interfaces (`IClaimRepository`) so multiple implementations can be swapped (in-memory → database).
3. **Top-Down Design:** Start with what the API exposes (DTOs), then build layers beneath.

**Rationale:**
- Ensures the API contract is solid before committing to database schema.
- Allows testing with in-memory data before setting up PostgreSQL.

### Phase 2: Data Persistence (Entity Framework Core)

**What Was Built:**
- Installed NuGet packages (`EntityFrameworkCore`, `Npgsql`)
- Created `ClaimsDbContext` (EF Core context)
- Configured PostgreSQL connection string in `appsettings.json`
- Registered DbContext in `Program.cs` dependency injection
- Generated initial migration (`dotnet ef migrations add InitialCreate`)
- Replaced in-memory repository with EF Core implementation

**Why This Order:**
1. **DbContext First:** Central hub for database operations; must exist before repositories can use it.
2. **Migrations After Models:** EF Core generates migrations from model definitions.
3. **Repository Last:** Repository implementation depends on DbContext being configured.

**Rationale:**
- Migrations track schema history (like Git for databases).
- Swapping repository implementation (in-memory → EF Core) validates abstraction works.

### Phase 3: Containerization (Docker)

**What Was Built:**
- PostgreSQL defined in `docker-compose.yml`
- `Dockerfile` for claims-service (multi-stage build)
- Environment variables in `.env.example`
- Auto-migration on startup in `Program.cs`

**Why This Order:**
1. **Database First:** Application depends on database; must start before app.
2. **Health Checks:** Ensure PostgreSQL is ready before claims-service attempts connection.
3. **Auto-Migrations:** Simplifies developer experience (no manual `dotnet ef` commands in containers).

**Rationale:**
- Docker Compose `depends_on` with `condition: service_healthy` prevents race conditions.
- Auto-migrations ensure database schema is always in sync with code.

### Phase 4: Microservice Integration (Audit Service)

**What Was Built:**
- FastAPI application (`main.py`)
- Pydantic models for request/response validation
- In-memory storage (Python list)
- `Dockerfile` for audit-service
- HTTP client in claims-service (`IHttpClientFactory`)
- Service-to-service call in `ClaimService.CreateClaimAsync`
- Updated `docker-compose.yml` to include audit-service

**Why This Order:**
1. **Audit Service Standalone:** Build and test independently before integrating.
2. **HTTP Client Last:** Ensure audit-service is running before claims-service tries to call it.
3. **Graceful Degradation:** Claims still work if audit fails (warning logged, not exception).

**Rationale:**
- Demonstrates polyglot architecture (C# + Python in same system).
- Fire-and-forget audit pattern (claims shouldn't fail due to audit errors).

### Key Takeaway: **Bottom-Up for Infrastructure, Top-Down for Features**

- **Infrastructure (Database, Docker):** Built bottom-up (foundation first).
- **Features (APIs, Services):** Built top-down (define contract first, implement later).

---

## Prerequisites

Before running this application, ensure the following tools are installed:

### Required Software

| Tool | Version | Purpose | Installation |
|------|---------|---------|--------------|
| **.NET SDK** | 10.0+ | Compile and run C# applications | https://dotnet.microsoft.com/download |
| **Docker** | 24.0+ | Container runtime | https://docs.docker.com/get-docker/ |
| **Docker Compose** | 2.20+ | Multi-container orchestration | Included with Docker Desktop |
| **Python** | 3.11+ | *(Optional)* Local audit-service development | https://www.python.org/downloads/ |
| **curl** or **Postman** | Any | API testing | Built-in on Linux/Mac, Postman GUI alternative |
| **jq** | 1.6+ | *(Optional)* JSON formatting in terminal | `apt install jq` / `brew install jq` |

### Verify Installation

```bash
# Check .NET version
dotnet --version

# Check Docker version
docker --version
docker-compose --version

# Check Python version (optional)
python3 --version

# Check curl
curl --version

# Check jq (optional)
jq --version
```

---

## Local Setup & Installation

### Environment Options

This project can be run in two environments:

1. **GitHub Codespaces** (Recommended for quick start)
   - Pre-configured dev container
   - No local setup required
   - Automatic port forwarding
   - See [Codespaces Setup](#codespaces-setup) below

2. **Local Machine** (For traditional development)
   - Requires Docker Desktop
   - Full control over environment
   - See [Local Setup](#local-machine-setup) below

---

### Codespaces Setup

**Step 1: Open in Codespaces**
- Click "Code" → "Codespaces" → "Create codespace on main"
- Wait for container to build (happens automatically)

**Step 2: Configure Environment**
```bash
cd docker
cp .env.example .env
# ⚠️  IMPORTANT: .env must be in docker/ folder
```

**Step 3: Start Services**
```bash
docker compose -f docker-compose.yml up -d --build
```

**Step 4: Make Ports Public**

GitHub Codespaces ports are private by default. To access services in your browser:

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type: "Ports: Focus on Ports View"
3. Press Enter
4. In the Ports panel, find ports **8000** and **5115**
5. Right-click each port → **Port Visibility** → **Public**

**Step 5: Access Services**

Get your Codespace name by running:
```bash
echo $CODESPACE_NAME
```

Then access services in your browser (replace `YOUR-CODESPACE-NAME`):

| Service | URL |
|---------|-----|
| **Swagger UI (Interactive)** | `https://YOUR-CODESPACE-NAME-8000.app.github.dev/docs` |
| **Claims API** | `https://YOUR-CODESPACE-NAME-5115.app.github.dev/api/claims` |
| **Health Check** | `https://YOUR-CODESPACE-NAME-5115.app.github.dev/health` |
| **Audit API** | `https://YOUR-CODESPACE-NAME-8000.app.github.dev/audit` |

**Alternative: Use Terminal Commands**

These work immediately without port configuration:
```bash
# Verify health
curl http://localhost:5115/health | jq .
curl http://localhost:8000/health | jq .

# Create test claim
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{
    "memberId": "MBR-TEST",
    "amount": 100.00,
    "currency": "USD"
  }' | jq .

# List all claims
curl http://localhost:5115/api/claims | jq .

# View audit events
curl http://localhost:8000/audit | jq .
```

---

### Local Machine Setup

**Step 1: Clone the Repository**

```bash
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app
```

**Step 2: Create Environment Configuration**

```bash
# Navigate to docker folder and copy the example environment file
cd docker
cp .env.example .env
# ⚠️  IMPORTANT: .env must be in docker/ folder, not in repository root

# Edit if needed (default values are fine for local development)
cat .env
```

**Environment Variables Explained:**
- `POSTGRES_USER`: PostgreSQL superuser username
- `POSTGRES_PASSWORD`: PostgreSQL superuser password
- `POSTGRES_DB`: Database name for claims data
- `POSTGRES_PORT`: Port exposed on host machine (default: 5432)
- `ASPNETCORE_ENVIRONMENT`: .NET environment (Development/Production)
- `AUDIT_SERVICE_URL`: Base URL for audit-service (used by claims-service)

### Step 3: Build and Start Services

```bash
# From docker/ folder (where .env is located)
docker compose -f docker-compose.yml up -d --build

# OR using old syntax (still works)
docker-compose up -d --build
```

**What Happens:**
1. **PostgreSQL** starts first (port 5432)
2. **Audit Service** starts (port 8000)
3. **Claims Service** waits for PostgreSQL health check, then starts (port 5115)
4. **Auto-Migration:** Claims-service automatically applies EF Core migrations on startup

**Expected Output:**
```
[+] Building 45.2s (28/28) FINISHED
[+] Running 4/4
 ✔ Network docker_claimsops-network    Created
 ✔ Container claimsops-postgres        Healthy
 ✔ Container claimsops-audit-service   Started
 ✔ Container claimsops-claims-service  Started
```

### Step 4: Verify Services Are Running

```bash
# Check container status
docker compose -f docker-compose.yml ps

# Expected output shows all services "Up" or "Up (healthy)"
```

**Access Services:**

Open in your browser:
- **Swagger UI (Interactive API):** http://localhost:8000/docs
- **Claims API:** http://localhost:5115/api/claims
- **Health Check:** http://localhost:5115/health

Or test with curl:
```bash
curl http://localhost:5115/health | jq .
curl http://localhost:8000/health | jq .
curl http://localhost:5115/api/claims | jq .
```

### Step 5: View Logs (Optional)

```bash
# All services
docker compose -f docker-compose.yml logs -f

# Specific service
docker compose -f docker-compose.yml logs -f claims-service
docker compose -f docker-compose.yml logs -f audit-service
docker compose -f docker-compose.yml logs -f postgres
```

---

## Running the Application

### Starting Services

```bash
cd docker
docker compose -f docker-compose.yml up -d
```

### Stopping Services

```bash
# Stop without removing containers
docker compose -f docker-compose.yml stop

# Stop and remove containers (data persists in volumes)
docker compose -f docker-compose.yml down

# Stop, remove containers, and DELETE ALL DATA
docker compose -f docker-compose.yml down -v
```

### Rebuilding After Code Changes

```bash
# Rebuild specific service
docker compose -f docker-compose.yml up -d --build claims-service

# Rebuild all services
docker compose -f docker-compose.yml up -d --build
```

---

## Smoke Tests & Verification

Run these tests after starting services to verify everything works.

### Test 1: Health Checks

```bash
# Claims Service health
curl http://localhost:5115/health | jq .

# Expected:
{
  "status": "healthy",
  "service": "claims-service",
  "timestamp": "2026-02-27T00:00:00.000Z"
}

# Audit Service health
curl http://localhost:8000/health | jq .

# Expected:
{
  "status": "healthy",
  "service": "audit-service",
  "timestamp": "2026-02-27T00:00:00.000000"
}
```

### Test 2: Create a Claim

```bash
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{
    "memberId": "MBR-12345",
    "amount": 250.50,
    "currency": "USD"
  }' | jq .

# Expected response (201 Created):
{
  "id": "7c5ee6b1-540c-4daf-a9f1-3a72c94be865",
  "memberId": "MBR-12345",
  "status": "Draft",
  "amount": 250.50,
  "currency": "USD",
  "createdAt": "2026-02-27T00:00:00.000Z"
}
```

**What Happens Internally:**
1. Request validated by ASP.NET Core model validation
2. `ClaimsController` receives request
3. `ClaimService` creates `Claim` entity
4. `ClaimRepository` persists to PostgreSQL via EF Core
5. `ClaimService` calls audit-service via HTTP
6. Audit-service records event in memory
7. `ClaimResponse` DTO returned to client

### Test 3: List All Claims

```bash
curl http://localhost:5115/api/claims | jq .

# Expected: Array of claims
[
  {
    "id": "7c5ee6b1-540c-4daf-a9f1-3a72c94be865",
    "memberId": "MBR-12345",
    "status": "Draft",
    "amount": 250.50,
    "currency": "USD",
    "createdAt": "2026-02-27T00:00:00.000Z"
  }
]
```

### Test 4: Get Claim By ID

```bash
# Replace {id} with actual claim ID from previous test
curl http://localhost:5115/api/claims/7c5ee6b1-540c-4daf-a9f1-3a72c94be865 | jq .

# Expected: Single claim object (200 OK)
# Or: 404 Not Found if ID doesn't exist
```

### Test 5: Verify Audit Event Was Recorded

```bash
# Get all audit events
curl http://localhost:8000/audit | jq .

# Expected:
[
  {
    "id": "4ecd95e9-e26a-4579-bd67-ffb3d5a7315e",
    "claim_id": "7c5ee6b1-540c-4daf-a9f1-3a72c94be865",
    "event_type": "created",
    "user_id": "system",
    "details": "Claim created for member MBR-12345",
    "timestamp": "2026-02-27T00:00:00.000000"
  }
]

# Filter by specific claim ID
curl "http://localhost:8000/audit?claim_id=7c5ee6b1-540c-4daf-a9f1-3a72c94be865" | jq .
```

### Test 6: Verify Data Persists in PostgreSQL

```bash
# Connect to PostgreSQL container
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db

# Run SQL query
SELECT * FROM "Claims";

# Exit PostgreSQL
\q
```

---

## Common Commands Reference

### Docker Compose

```bash
# Start all services (modern syntax - recommended)
docker compose -f docker/docker-compose.yml up -d

# Start with build (after code changes)
docker compose -f docker/docker-compose.yml up -d --build

# Stop services
docker compose -f docker/docker-compose.yml stop

# Stop and remove containers
docker compose -f docker/docker-compose.yml down

# Stop, remove containers, and delete volumes (full reset)
docker compose -f docker/docker-compose.yml down -v

# View logs
docker compose -f docker/docker-compose.yml logs -f [service-name]

# Check service status
docker compose -f docker/docker-compose.yml ps

# Restart specific service
docker compose -f docker/docker-compose.yml restart claims-service

# Execute command in running container
docker exec -it claimsops-claims-service /bin/bash

# Old syntax (still works if you're in docker/ folder)
cd docker
docker-compose up -d
```

### .NET Commands (Claims Service)

```bash
cd services/claims-service

# Restore NuGet packages
dotnet restore

# Build project
dotnet build

# Run locally (without Docker)
dotnet run

# Clean build artifacts
dotnet clean

# Add NuGet package
dotnet add package PackageName

# Remove NuGet package
dotnet remove package PackageName

# List installed packages
dotnet list package

# Create EF Core migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script
```

### Python Commands (Audit Service)

```bash
cd services/audit-service

# Create virtual environment (local development)
python3 -m venv venv

# Activate virtual environment
source venv/bin/activate  # Linux/Mac
venv\Scripts\activate     # Windows

# Install dependencies
pip install -r requirements.txt

# Run locally (without Docker)
uvicorn main:app --reload --port 8000

# Freeze dependencies (after installing new packages)
pip freeze > requirements.txt

# Deactivate virtual environment
deactivate
```

### PostgreSQL Commands

```bash
# Connect to database (use actual credentials from docker/.env)
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db

# Inside psql:
\dt                # List tables
\d "Claims"        # Describe Claims table
SELECT * FROM "Claims";  # Query data
\q                 # Quit

# Create database backup
docker exec claimsops-postgres pg_dump -U claimsops_user claimsops_db > backup.sql

# Restore from backup
cat backup.sql | docker exec -i claimsops-postgres psql -U claimsops_user -d claimsops_db
```

### Git Workflow

```bash
# Check current branch
git branch --show-current

# Create feature branch
git checkout main
git pull origin main
git checkout -b feature/my-feature

# Stage and commit changes
git add .
git commit -m "feat: description of changes"

# Push to remote (first time)
git push -u origin feature/my-feature

# Push subsequent changes
git push
```

---

## Troubleshooting

### Issue: "Port already in use"

**Symptom:**
```
Error: bind: address already in use
```

**Solution:**
```bash
# Find process using port 5115 (claims-service)
lsof -i :5115
kill -9 <PID>

# Or use different port in docker-compose.yml
ports:
  - "5116:5115"  # Host:Container
```

### Issue: "Database connection failed"

**Symptom:**
```
Npgsql.PostgresException: password authentication failed for user "postgres"
```

**Solution:**
```bash
# Reset PostgreSQL container and volumes
cd docker
docker-compose down -v
docker-compose up -d postgres

# Wait 10 seconds for initialization
sleep 10

# Restart claims-service
docker-compose up -d claims-service
```

### Issue: "Migrations not applied"

**Symptom:**
```
relation "Claims" does not exist
```

**Solution:**
```bash
# Check if auto-migration ran
docker-compose logs claims-service | grep "migration"

# Manually apply migrations
cd services/claims-service
dotnet ef database update

# Or rebuild container (auto-migration runs on startup)
cd docker
docker-compose up -d --build claims-service
```

### Issue: "Audit service not responding"

**Symptom:**
Claims created but no audit events recorded.

**Solution:**
```bash
# Check audit-service logs
docker-compose logs audit-service

# Verify audit-service is running
docker-compose ps

# Check health endpoint
curl http://localhost:8000/health

# Restart audit-service
docker-compose restart audit-service
```

---

## Development Workflow

### Adding a New Endpoint to Claims Service

1. **Define DTO** (if needed): `DTOs/NewRequest.cs`, `DTOs/NewResponse.cs`
2. **Add Repository Method**: `Repositories/IClaimRepository.cs` → `ClaimRepository.cs`
3. **Add Service Method**: `Services/IClaimService.cs` → `ClaimService.cs`
4. **Add Controller Action**: `Controllers/ClaimsController.cs`
5. **Test Locally**: `dotnet run` or `docker-compose up -d --build claims-service`
6. **Commit**: `git add . && git commit -m "feat: add new endpoint"`

### Making Database Schema Changes

1. **Modify Model**: Update `Models/Claim.cs`
2. **Create Migration**: `dotnet ef migrations add DescriptiveNameOfChange`
3. **Review Migration**: Check `Migrations/YYYYMMDDHHMMSS_DescriptiveNameOfChange.cs`
4. **Apply Migration**: `dotnet ef database update` (or restart Docker container)
5. **Commit Migration Files**: `git add Migrations/ && git commit -m "chore: add migration for XYZ"`

### Testing Changes Without Rebuilding Containers

```bash
# Stop container
docker-compose stop claims-service

# Run locally
cd services/claims-service
dotnet run

# API now accessible at http://localhost:5115
# Uses local .env or appsettings.json (configure connection string)
```

---

## API Documentation

### Claims Service Endpoints

| Method | Endpoint | Description | Status Codes |
|--------|----------|-------------|--------------|
| GET | `/health` | Health check | 200 OK |
| POST | `/api/claims` | Create new claim | 201 Created, 400 Bad Request |
| GET | `/api/claims` | List all claims | 200 OK |
| GET | `/api/claims/{id}` | Get claim by ID | 200 OK, 404 Not Found |

### Audit Service Endpoints

| Method | Endpoint | Description | Status Codes |
|--------|----------|-------------|--------------|
| GET | `/health` | Health check | 200 OK |
| POST | `/audit` | Record audit event | 201 Created, 422 Validation Error |
| GET | `/audit` | List all events | 200 OK |
| GET | `/audit?claim_id={id}` | Filter by claim ID | 200 OK |

### Interactive API Documentation

- **Claims Service Swagger:** http://localhost:5115/swagger (if enabled in development)
- **Audit Service Swagger:** http://localhost:8000/docs (FastAPI auto-generated)

---

## Next Steps & Future Enhancements

### Immediate Improvements

1. **Error Handling Middleware**: Centralized exception handling in claims-service
2. **Logging**: Structured logging with Serilog (claims-service) and Python logging (audit-service)
3. **Validation**: FluentValidation for complex business rules
4. **Tests**: Unit tests (xUnit for C#, pytest for Python)

### Medium-Term Enhancements

1. **Audit Service Persistence**: Replace in-memory list with PostgreSQL or MongoDB
2. **Authentication**: JWT bearer tokens for API security
3. **Frontend**: Next.js dashboard for viewing/creating claims
4. **API Gateway**: Nginx reverse proxy with rate limiting

### Long-Term Features

1. **Claim State Machine**: Workflow engine for Draft → Submitted → Approved transitions
2. **Async Communication**: Replace HTTP with message queue (RabbitMQ/Kafka)
3. **Observability**: Distributed tracing (OpenTelemetry), metrics (Prometheus)
4. **CI/CD Pipeline**: GitHub Actions for automated testing and deployment

---

## Glossary

| Term | Definition |
|------|------------|
| **ASGI** | Async Server Gateway Interface - Python standard for async web servers |
| **DTO** | Data Transfer Object - Simple objects for moving data between layers |
| **ORM** | Object-Relational Mapper - Converts between objects and database tables |
| **DI** | Dependency Injection - Design pattern for managing object dependencies |
| **CRUD** | Create, Read, Update, Delete - Basic data operations |
| **MVP** | Minimum Viable Product - Simplest version with core features |
| **ACID** | Atomicity, Consistency, Isolation, Durability - Database transaction properties |
| **REST** | Representational State Transfer - Architectural style for web APIs |

---

**Document Maintained By:** Engineering Team  
**Questions/Issues:** Create GitHub issue or contact tech lead
