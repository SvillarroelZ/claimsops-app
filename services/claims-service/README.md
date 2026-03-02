# Claims Service

ASP.NET Core Web API for managing insurance claims.

## Overview

The Claims Service is the main backend API for the ClaimsOps application. It handles all claim-related operations and communicates with the Audit Service to log events.

## Architecture

The service follows a layered architecture pattern:

```
HTTP Request
    |
    v
[Controllers]     - Receive HTTP requests, validate input, return responses
    |
    v
[Services]        - Business logic, orchestration, validation rules
    |
    v
[Repositories]    - Data access layer, database operations via Entity Framework
    |
    v
[PostgreSQL]      - Data persistence
```

### Directory Structure

| Directory | Purpose |
|-----------|---------|
| `Controllers/` | API endpoints - handle HTTP requests and responses |
| `Services/` | Business logic - rules, calculations, orchestration |
| `Repositories/` | Data access - Entity Framework database operations |
| `Models/` | Domain entities - represent database tables |
| `DTOs/` | Data Transfer Objects - request/response contracts |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Service health status |
| GET | `/api/claims` | List all claims |
| GET | `/api/claims/{id}` | Get specific claim by ID |
| POST | `/api/claims` | Create a new claim |
| PUT | `/api/claims/{id}` | Update an existing claim |
| DELETE | `/api/claims/{id}` | Delete a claim |

## Configuration

### appsettings.json

| Setting | Description |
|---------|-------------|
| `Logging:LogLevel` | Minimum log level (Information, Warning, Error) |
| `ServiceSettings:ServiceName` | Service identifier for logging and health checks |
| `ServiceSettings:AuditServiceUrl` | URL of the audit-service for logging events |
| `ConnectionStrings:DefaultConnection` | PostgreSQL database connection string |
| `Cors:AllowedOrigins` | Frontend URLs allowed to call this API |

## Running Locally

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL (via Docker Compose in /docker directory)

### Commands

```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the service (starts on http://localhost:5115)
dotnet run

# Run with hot reload (auto-restart on file changes)
dotnet watch run
```

### Verify Service is Running

```bash
curl http://localhost:5115/health
```

Expected response:
```json
{
  "status": "healthy",
  "service": "claims-service",
  "timestamp": "2026-02-19T00:00:00.000Z"
}
```

## Development

### Adding a New Endpoint

1. Create or update a Controller in `Controllers/`
2. Add DTOs in `DTOs/` for request/response types
3. Implement business logic in `Services/`
4. Add data access in `Repositories/` if needed

### Logging

Use `ILogger<T>` injected via constructor:

```csharp
_logger.LogInformation("Processing claim {ClaimId}", claimId);
_logger.LogWarning("Claim {ClaimId} validation failed", claimId);
_logger.LogError(ex, "Error processing claim {ClaimId}", claimId);
```

## Testing

```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```
