# ClaimsOps Application

Enterprise-grade insurance claims management system built with microservices architecture.

## Architecture

```
Frontend (Next.js/React/TypeScript)
    ↓
Claims Service (C# .NET Web API)
    ↓
Audit Service (Python FastAPI)
    ↓
PostgreSQL Database
```

## Tech Stack

- **Frontend**: Next.js 15, React 19, TypeScript
- **Backend**: C# .NET 10.0 Web API
- **Audit Service**: Python 3.12, FastAPI
- **Database**: PostgreSQL 15
- **Container**: Docker & Docker Compose
- **Testing**: xUnit (C#), pytest (Python), Jest (Frontend)

## Project Structure

```
claimsops-app/
├── frontend/              # Next.js application
├── services/
│   ├── claims-service/   # .NET Web API (main backend)
│   └── audit-service/    # Python FastAPI (audit events)
├── docker/               # Docker Compose setup
│   ├── docker-compose.yml
│   ├── .env.example      # Environment template
│   └── README.md
├── docs/
│   ├── architecture.md
│   └── security.md
├── scripts/              # Utility scripts
└── README.md
```

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Python 3.12+](https://www.python.org/downloads/)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/SvillarroelZ/claimsops-app.git
   cd claimsops-app
   ```

2. **Setup environment variables**
   ```bash
   cd docker
   cp .env.example .env
   # Edit .env and set POSTGRES_PASSWORD (see security.md)
   ```

3. **Start PostgreSQL**
   ```bash
   docker-compose up -d
   ```

4. **Run Claims Service**
   ```bash
   cd ../services/claims-service
   dotnet restore
   dotnet run
   # Service runs on http://localhost:5115
   ```

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
