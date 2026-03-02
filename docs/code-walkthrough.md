# Code Walkthrough: POST /api/claims Request Lifecycle

This document describes, in execution order, what happens when a client sends `POST /api/claims`.

## Purpose

- Explain what each component does.
- Explain why each step exists.
- Show where data originates, where it is transformed, and where it is stored.
- Clarify which steps are blocking and which are best-effort.

## End-to-End Flow Summary

```text
Client
  -> ClaimsController (HTTP boundary)
  -> ClaimService (business orchestration)
  -> ClaimRepository (persistence)
  -> PostgreSQL (source of record)
  -> ClaimService (best-effort audit dispatch)
  -> Audit Service (in-memory event store)
  -> ClaimsController (201 Created response)
  -> Client
```

## Data Origin and Destination

| Stage | Data Form | Source | Destination | Purpose |
| --- | --- | --- | --- | --- |
| Request intake | JSON body | External client | `CreateClaimRequest` DTO | Validate input contract |
| Domain mapping | Domain entity | `CreateClaimRequest` | `Claim` | Apply defaults and internal semantics |
| Persistence | Relational row | `Claim` | PostgreSQL `Claims` table | Durable storage of claim record |
| API response | Response DTO | Persisted `Claim` | Client JSON payload | Return stable API contract |
| Audit event | JSON payload | `ClaimService` | Audit Service `POST /audit` | Record operational event for traceability |

## Step 1: HTTP request arrives at the controller

**Component:** `ClaimsController.CreateClaim`

### Step 1 behavior

- Receives JSON payload from the client.
- Relies on ASP.NET Core model binding and data annotations for request validation.
- Delegates business logic to the service layer.
- Returns `201 Created` with a location for the new resource.

### Step 1 rationale

- Keeps transport concerns (HTTP, status codes, routing) separate from business rules.

### Step 1 input example

```http
POST /api/claims HTTP/1.1
Host: localhost:5115
Content-Type: application/json

{
  "memberId": "MBR-12345",
  "amount": 500.00,
  "currency": "USD"
}
```

## Step 2: Service orchestration and domain mapping

**Component:** `ClaimService.CreateClaimAsync`

### Step 2 behavior

- Creates a new domain object.
- Assigns defaults and system-generated values.
  - `Id`: `Guid.NewGuid()`
  - `Status`: `Draft`
  - `CreatedAt`: UTC timestamp
- Calls repository to persist the record.
- Triggers audit recording as a best-effort side effect.
- Maps persisted entity to API response DTO.

### Step 2 rationale

- Centralizes business behavior so controllers remain thin and testable.

### Step 2 mapping intent

| Field | Source | Rule |
| --- | --- | --- |
| `MemberId` | Request | Required, length-constrained by DTO validation |
| `Amount` | Request | Must be greater than zero |
| `Currency` | Request | Optional in request, normalized by service defaults when needed |
| `Status` | Service | Initialized to `Draft` |
| `CreatedAt` | Service | Always UTC |

## Step 3: Repository persists data to PostgreSQL

**Component:** `ClaimRepository.CreateAsync`

### Step 3 behavior

- Adds entity to EF Core `DbContext`.
- Executes `SaveChangesAsync()`.
- Returns the persisted entity.

### Step 3 rationale

- Encapsulates data access mechanics behind an interface.
- Allows service logic to remain database-agnostic.

### Step 3 data movement

- From: in-memory `Claim` entity.
- To: durable row in PostgreSQL table `Claims`.

## Step 4: Service sends audit event (best-effort)

**Component:** `ClaimService.RecordAuditEventAsync`

### Step 4 behavior

- Reads audit base URL from configuration (`AuditService__BaseUrl`).
- Builds event payload.
- Sends HTTP `POST` to `/audit` using `HttpClient`.
- Logs failures as warnings without failing claim creation.

### Step 4 rationale

- Captures operational history while preserving write-path availability for claims.

### Step 4 payload example

```json
{
  "claim_id": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "created",
  "user_id": "system",
  "details": "Claim created for member MBR-12345"
}
```

## Step 5: Audit service stores the event in memory

**Component:** `audit-service/main.py` endpoint `POST /audit`

### Step 5 behavior

- Accepts event payload.
- Adds event metadata (`id`, `timestamp`).
- Appends record to in-memory collection.
- Returns `201 Created`.

### Step 5 rationale

- Provides immediate, lightweight audit capability for MVP scope.

### Step 5 current limitation

- Storage is in memory only; events are lost when the container restarts.

## Step 6: Controller returns final API response

### Step 6 response contract

- Status: `201 Created`
- Headers: `Location: /api/claims/{id}`
- Body: `ClaimResponse`

### Step 6 rationale

- Confirms successful creation and provides resource address for follow-up reads.

## Blocking vs non-blocking behavior

| Operation | Blocking for client response? | Reason |
| --- | --- | --- |
| Request validation | Yes | Invalid input must stop processing |
| Claim persistence | Yes | Claim must be durably stored before success response |
| Audit dispatch | No (best-effort) | Audit failure should not block claim creation |

## Failure scenarios and outcomes

### Invalid input

- Example: `amount <= 0` or missing `memberId`.
- Outcome: `400 Bad Request` from model validation.
- Purpose: reject invalid business inputs at API boundary.

### Database unavailable

- Outcome: `500 Internal Server Error`.
- Purpose: avoid returning false success when source-of-record write fails.

### Audit service unavailable

- Outcome: claim still returns `201 Created`; warning is logged.
- Purpose: preserve primary transaction availability.

## Operational verification

### Verify claim persistence

```bash
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db -c "SELECT * FROM \"Claims\";"
```

### Verify audit events

```bash
curl -s http://localhost:8000/audit | jq .
```

### Verify service logs

```bash
docker compose -f docker/docker-compose.yml logs -f claims-service
docker compose -f docker/docker-compose.yml logs -f audit-service
```

## Key files involved

```text
services/claims-service/Controllers/ClaimsController.cs
services/claims-service/Services/ClaimService.cs
services/claims-service/Repositories/ClaimRepository.cs
services/claims-service/Data/ClaimsDbContext.cs
services/claims-service/DTOs/CreateClaimRequest.cs
services/claims-service/DTOs/ClaimResponse.cs
services/claims-service/Models/Claim.cs
services/claims-service/Models/ClaimStatus.cs
services/audit-service/main.py
```

## Design rationale

- Layered architecture separates transport, business logic, and persistence responsibilities.
- DTO boundaries protect public API contracts from internal model changes.
- Best-effort audit integration balances traceability with write-path reliability.
- UTC timestamps and generated IDs ensure consistent cross-service event correlation.
