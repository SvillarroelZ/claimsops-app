# ClaimsOps Application

[**English**](#) | [**Español**](README.es.md)

Enterprise-grade insurance claims management system built with microservices architecture.

**Status:** MVP Complete  
**Version:** 1.0.0

## Quick Start

### Option 1: GitHub Codespaces (Recommended)

1. Open this repository in Codespaces
2. In the terminal, set up environment variables:
   ```bash
   cd docker
   cp .env.example .env
   ```

3. Start all services:
   ```bash
   docker compose -f docker-compose.yml up -d --build
   ```

4. **Make ports public** (required for browser access):
   - Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
   - Type: "Ports: Focus on Ports View"
   - Press Enter
   - Right-click on port **8000** → Port Visibility → **Public**
   - Right-click on port **5115** → Port Visibility → **Public**

5. **Access services in your browser**:
   - Get your Codespace name: run `echo $CODESPACE_NAME`
   - Replace `YOUR-CODESPACE-NAME` with the actual name
   - **Swagger UI (Interactive API):** `https://YOUR-CODESPACE-NAME-8000.app.github.dev/docs`
   - **Claims API:** `https://YOUR-CODESPACE-NAME-5115.app.github.dev/api/claims`
   - **Health Check:** `https://YOUR-CODESPACE-NAME-5115.app.github.dev/health`

6. **From the terminal** (always works):
   ```bash
   # Verify services are running
   curl http://localhost:5115/health | jq .
   curl http://localhost:8000/health | jq .
   
   # Create test claim
   curl -X POST http://localhost:5115/api/claims \
     -H "Content-Type: application/json" \
     -d '{"memberId":"MBR-001","amount":250.50,"currency":"USD"}' | jq .
   
   # List all claims
   curl http://localhost:5115/api/claims | jq .
   ```

### Option 2: Local Development (Mac / Linux)

```bash
# Clone repository
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app

# Configure environment (IMPORTANT: creates .env in docker/ folder)
cd docker
cp .env.example .env

# Start all services
docker compose -f docker-compose.yml up -d --build

# Verify services are running
sleep 10
curl http://localhost:5115/health | jq .
curl http://localhost:8000/health | jq .

# Create a test claim
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"MBR-001","amount":250.50,"currency":"USD"}' | jq .

# View Swagger documentation
# Open browser to: http://localhost:8000/docs
```

## Architecture

```
┌─────────────────────────────────────────────┐
│          Docker Compose Network              │
├─────────────────────────────────────────────┤
│                                              │
│  ┌──────────────┐      ┌──────────────┐    │
│  │  PostgreSQL  │◄─────┤ Claims-Svc   │    │
│  │   (Port 5432)│      │  (Port 5115) │    │
│  └──────────────┘      └──────┬───────┘    │
│                                │             │
│                                │ HTTP        │
│                                ▼             │
│                        ┌──────────────┐     │
│                        │  Audit-Svc   │     │
│                        │  (Port 8000) │     │
│                        └──────────────┘     │
└─────────────────────────────────────────────┘
```

## Tech Stack

### Backend Services

| Component | Technology | Port | Purpose |
|-----------|-----------|------|---------|
| **Claims Service** | C# .NET 10.0 | 5115 | Main API for claims management |
| **Audit Service** | Python 3.11 + FastAPI | 8000 | Audit event recording |
| **Database** | PostgreSQL 15 | 5432 | Data persistence |

### Key Libraries & Tools

- **Entity Framework Core 10.0** - ORM with code-first migrations
- **Npgsql** - PostgreSQL driver for .NET
- **FastAPI** - Modern async Python web framework
- **Uvicorn** - ASGI server for Python
- **Docker Compose** - Multi-container orchestration

## Project Structure

```
claimsops-app/
├── services/
│   ├── claims-service/        # C# .NET Web API
│   │   ├── Controllers/       # HTTP endpoints
│   │   ├── Services/          # Business logic
│   │   ├── Repositories/      # Data access (EF Core)
│   │   ├── Models/            # Domain entities
│   │   ├── DTOs/              # API contracts
│   │   ├── Data/              # DbContext
│   │   ├── Migrations/        # EF Core migrations
│   │   └── Dockerfile
│   │
│   └── audit-service/         # Python FastAPI
│       ├── main.py            # Application & endpoints
│       ├── requirements.txt   # Python dependencies
│       └── Dockerfile
│
├── docker/
│   ├── docker-compose.yml     # Service orchestration
│   ├── init.sql               # PostgreSQL setup
│   └── README.md
│
├── docs/
│   ├── runbook.md             # Complete technical guide
│   └── security.md            # Security documentation
│
├── .env.example               # Environment template
├── .gitignore
└── README.md
```

## API Endpoints

### Claims Service (C# .NET)

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| GET | `/health` | Health check | 200 OK |
| POST | `/api/claims` | Create new claim | 201 Created |
| GET | `/api/claims` | List all claims | 200 OK |
| GET | `/api/claims/{id}` | Get claim by ID | 200 OK / 404 Not Found |

### Audit Service (Python FastAPI)

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| GET | `/health` | Health check | 200 OK |
| POST | `/audit` | Record audit event | 201 Created |
| GET | `/audit` | List all events | 200 OK |
| GET | `/audit?claim_id={id}` | Filter by claim | 200 OK |

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (for local development)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required)
- [Python 3.11+](https://www.python.org/downloads/) (for local development)
- curl or [Postman](https://www.postman.com/) (for testing)

### Installation & Setup

**1. Clone and Configure** (your local machine or Codespace)

```bash
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app
cd docker
cp .env.example .env
# IMPORTANT: place .env in docker/ folder, not in the repository root
```

**2. Start All Services** (from docker/ folder)

```bash
# Modern syntax (recommended)
docker compose -f docker-compose.yml up -d --build

# OR old syntax (still works)
docker-compose up -d --build
```

**3. Verify Health**

```bash
# Wait 10 seconds for services to start
sleep 10

# Check claims-service health
curl http://localhost:5115/health | jq .

# Check audit-service health
curl http://localhost:8000/health | jq .
```

**4. Test Complete Flow** (Create → Audit → List)

```bash
# Create a claim (persists to PostgreSQL + triggers audit event)
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{
    "memberId": "MBR-12345",
    "amount": 500.00,
    "currency": "USD"
  }' | jq .

# List all claims from database
curl http://localhost:5115/api/claims | jq .

# View audit events recorded by audit-service
curl http://localhost:8000/audit | jq .
```

**5. Stopping Services**

```bash
# Stop services (keep data)
cd docker
docker compose -f docker-compose.yml stop

# Stop and remove containers (data persists in volumes)
docker compose -f docker-compose.yml down

# Full reset (removes all data and volumes)
docker compose -f docker-compose.yml down -v
```

## Development Workflow

### Running Locally (Without Docker)

**Claims Service:**

```bash
cd services/claims-service

# Ensure PostgreSQL is running
docker-compose up -d postgres

# Run the application
dotnet run

# API available at http://localhost:5115
```

**Audit Service:**

```bash
cd services/audit-service

# Create virtual environment
python3 -m venv venv
source venv/bin/activate  # Linux/Mac
venv\Scripts\activate     # Windows

# Install dependencies
pip install -r requirements.txt

# Run the application
uvicorn main:app --reload --port 8000

# API available at http://localhost:8000
```

### Making Database Changes

```bash
cd services/claims-service

# 1. Modify Models/Claim.cs
# 2. Create migration
dotnet ef migrations add DescriptiveName

# 3. Apply migration
dotnet ef database update

# 4. Rebuild container (auto-applies migrations on startup)
cd ../../docker
docker-compose up -d --build claims-service
```

### Adding New Endpoints

1. Define DTO in `DTOs/`
2. Add repository method in `Repositories/`
3. Add service method in `Services/`
4. Add controller action in `Controllers/`
5. Test locally with `dotnet run`
6. Commit changes

## Documentation

- **[Complete Technical Runbook](docs/runbook.md)** - System setup, operating procedures, and failure handling
- **[Security Documentation](docs/security.md)** - Secrets handling, validation controls, and deployment safeguards
- **[Docker Guide](docker/README.md)** - Container topology, network flow, and runtime operations

## Common Commands

```bash
# Docker Compose (modern syntax - recommended)
docker compose -f docker/docker-compose.yml up -d              # Start all services
docker compose -f docker/docker-compose.yml logs -f            # View all logs
docker compose -f docker/docker-compose.yml logs -f claims-service  # View specific service
docker compose -f docker/docker-compose.yml ps                 # Check service status
docker compose -f docker/docker-compose.yml restart claims-service  # Restart service
docker compose -f docker/docker-compose.yml down -v            # Full reset

# .NET Commands (from services/claims-service/)
dotnet build                      # Compile project
dotnet run                        # Run locally
dotnet ef migrations add Name     # Create migration
dotnet ef database update         # Apply migrations
dotnet add package PackageName    # Install NuGet package

# Python Commands (from services/audit-service/)
pip install -r requirements.txt   # Install dependencies
uvicorn main:app --reload         # Run with auto-reload
pip freeze > requirements.txt     # Update dependencies list

# Database Access (from any folder)
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db
```

## Testing

### Manual Smoke Tests

All tests documented in [docs/runbook.md](docs/runbook.md#smoke-tests--verification)

### Quick Test Script

```bash
#!/bin/bash
# test-mvp.sh

echo "Testing Claims Service..."
curl -s http://localhost:5115/health | jq .

echo "\nCreating claim..."
CLAIM=$(curl -s -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"MBR-TEST","amount":100.00,"currency":"USD"}')

echo $CLAIM | jq .
CLAIM_ID=$(echo $CLAIM | jq -r '.id')

echo "\nVerifying audit event..."
curl -s "http://localhost:8000/audit?claim_id=$CLAIM_ID" | jq .

echo "\nMVP test completed"
```

## Troubleshooting

### Port Already In Use

```bash
# Find process using port 5115
lsof -i :5115
kill -9 <PID>

# Or use different port
# Edit docker-compose.yml: "5116:5115"
```

### Database Connection Failed

```bash
# Reset PostgreSQL
docker-compose down -v postgres
docker-compose up -d postgres
sleep 10
docker-compose up -d claims-service
```

### Migrations Not Applied

```bash
# Check logs
docker-compose logs claims-service | grep migration

# Manually apply
cd services/claims-service
dotnet ef database update
```

See [docs/runbook.md](docs/runbook.md#troubleshooting) for more details.

## Technology Decisions

### Why .NET for Claims Service?
- Type safety and compile-time checking
- Enterprise maturity in regulated industries
- Rich ecosystem (EF Core, authentication, logging)
- High performance for CRUD operations

### Why Python/FastAPI for Audit Service?
- Rapid development (< 1 hour to build MVP)
- Minimal boilerplate for simple CRUD
- Auto-generated OpenAPI documentation
- Demonstrates polyglot microservices

### Why PostgreSQL?
- ACID compliance for financial transactions
- Free and open source (no licensing costs)
- JSON support for hybrid relational/document data
- Proven scalability

See [docs/runbook.md](docs/runbook.md#technology-stack-overview) for full rationale.

## Roadmap

### Phase 1: MVP (Completed)
- [x] Claims CRUD API with PostgreSQL persistence
- [x] Audit service with in-memory storage
- [x] Docker Compose orchestration
- [x] Service-to-service HTTP integration
- [x] Comprehensive documentation

### Phase 2: Enhancements (Future)
- [ ] Audit service database persistence
- [ ] Unit tests (xUnit + pytest)
- [ ] Error handling middleware
- [ ] Structured logging (Serilog)
- [ ] JWT authentication

### Phase 3: Frontend (Future)
- [ ] Next.js dashboard
- [ ] Create/list/view claims UI
- [ ] Typed API client

### Phase 4: Production Ready (Future)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Kubernetes manifests
- [ ] Monitoring (Prometheus + Grafana)
- [ ] Distributed tracing (OpenTelemetry)

## Contributing

1. Create feature branch: `git checkout -b feature/my-feature`
2. Make changes and test locally
3. Commit using Conventional Commits: `git commit -m "feat: add new feature"`
4. Push and create Pull Request

## License

MIT License - See LICENSE file for details

## Support

- **Documentation:** [docs/runbook.md](docs/runbook.md)
- **Issues:** https://github.com/SvillarroelZ/claimsops-app/issues
- **Contact:** Engineering Team

---

**Built with ❤️ as a complete MVP for enterprise insurance claims management**

5. **Verify health**
   ```bash
   curl http://localhost:5115/health
   ```

## Development

See individual service READMEs:
- [Claims Service](services/claims-service/README.md)
- [Docker Setup](docker/README.md)

## Documentation

- [Architecture Overview](docs/architecture.md)
- [Security Guidelines](docs/security.md)

## API Endpoints

### Claims Service

- `GET /health` - Health check

(More endpoints will be added in subsequent phases)

## Testing

```bash
# .NET tests
cd services/claims-service
dotnet test

# Python tests
cd services/audit-service
pytest

# Frontend tests
cd frontend
npm test
```

## Security

**Important:** Never commit `.env` files or secrets to version control.

See [Security Guidelines](docs/security.md) for details.

## Contributing

1. Create a feature branch from `main`
2. Make your changes
3. Ensure tests pass
4. Commit with conventional commits format
5. Push and create a Pull Request

### Commit Convention

```
feat: add new feature
fix: bug fix
chore: maintenance tasks
docs: documentation updates
refactor: code restructuring
test: add or update tests
```

## License

MIT License - See LICENSE file for details
