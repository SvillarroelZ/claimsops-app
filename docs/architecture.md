# Architecture Summary

## Services

- **claims-service (C# .NET)**
  - Exposes claims API (`/api/claims`, `/health`).
  - Implements controller -> service -> repository layering.
  - Persists claim data in PostgreSQL through EF Core.

- **audit-service (Python FastAPI)**
  - Exposes audit API (`/audit`, `/health`).
  - Receives audit events from claims-service over HTTP.
  - Stores events in memory for MVP.

- **postgres (PostgreSQL 15)**
  - Persistent storage for claim records.
  - Initialized via Docker Compose and EF Core migrations.

## Main runtime flow

1. Client sends `POST /api/claims` to claims-service.
2. claims-service validates request and saves claim in PostgreSQL.
3. claims-service sends `POST /audit` to audit-service.
4. claims-service returns `201 Created` response to client.

## Data ownership

- claims-service owns claim lifecycle and persistence.
- audit-service owns audit events.
- Integration is synchronous HTTP for simplicity in MVP.

## Future Improvements

- **Tests**
  - Add unit tests for service/repository logic.
  - Add integration tests for `POST /api/claims` end-to-end flow.

- **CI/CD**
  - Add GitHub Actions pipeline for build, test, and lint checks.
  - Add image build and deployment validation steps.

- **Audit persistence**
  - Replace in-memory storage in audit-service with PostgreSQL persistence.
  - Add migration/versioning strategy for audit schema.

- **Observability**
  - Add structured metrics (request count, latency, error rate).
  - Add distributed tracing across claims-service and audit-service.
  - Add centralized log aggregation and alerting rules.