# Code Walkthrough: POST /api/claims

This document explains, step by step, what happens when a client calls:

`POST /api/claims`

with body:

```json
{
  "memberId": "MBR-12345",
  "amount": 500.00,
  "currency": "USD"
}
```

## 1) HTTP request enters Claims Service

- The request reaches `ClaimsController.CreateClaim`.
- ASP.NET Core model binding maps JSON into `CreateClaimRequest`.
- DataAnnotations validation runs automatically (`[Required]`, `[Range]`, `[StringLength]`).
- If validation fails, framework returns `400 Bad Request` before service logic executes.

## 2) Controller delegates to business layer

- `ClaimsController` calls `_claimService.CreateClaimAsync(request)`.
- The controller does not implement business rules or persistence directly.
- This keeps endpoint logic thin and easy to explain.

## 3) Service builds the domain entity

Inside `ClaimService.CreateClaimAsync`:

- A new `Claim` entity is created.
- Service sets default fields:
  - `Id = Guid.NewGuid()`
  - `Status = Draft`
  - `CreatedAt = DateTime.UtcNow`
- Input fields from request are mapped (`MemberId`, `Amount`, `Currency`).

## 4) Repository persists to PostgreSQL

- Service calls `_repository.CreateAsync(claim)`.
- `ClaimRepository` uses `ClaimsDbContext` (EF Core).
- `_context.Claims.Add(claim)` marks entity for insert.
- `_context.SaveChangesAsync()` executes SQL `INSERT` in PostgreSQL.
- Persisted entity is returned to service.

## 5) Service calls audit-service via HTTP

Still in `CreateClaimAsync`, after DB save:

- Service calls `RecordAuditEventAsync(...)`.
- It reads `AuditService:BaseUrl` from configuration.
- It builds JSON payload and sends `POST {AuditServiceBaseUrl}/audit`.
- If audit call fails, it logs a warning and continues.
- Claim creation still succeeds (graceful degradation).

## 6) Service maps output DTO

- `ClaimService` maps entity to `ClaimResponse`.
- Status enum is converted to string for API readability.

## 7) Controller returns final API response

- `ClaimsController` returns `201 Created`.
- Uses `CreatedAtAction(nameof(GetClaimById), new { id = claim.Id }, claim)`.
- Response includes:
  - Created claim body (`ClaimResponse`)
  - `Location` header pointing to `GET /api/claims/{id}`

## End-to-end sequence summary

1. Client -> `POST /api/claims`
2. Controller validates + delegates
3. Service creates domain object
4. Repository stores claim in PostgreSQL
5. Service sends audit event to audit-service
6. Controller returns `201 Created`

## Expected response example

```json
{
  "id": "f9f8b2d6-0f7d-4c7f-8a4e-0f9f6f8f6c21",
  "memberId": "MBR-12345",
  "status": "Draft",
  "amount": 500.00,
  "currency": "USD",
  "createdAt": "2026-02-27T01:00:00Z"
}
```

## Why this flow is useful for technical study

- Clearly separates API, business logic, and data access.
- Demonstrates EF Core persistence path to PostgreSQL.
- Demonstrates service-to-service HTTP communication.
- Keeps behavior simple and realistic for MVP-level explanation.