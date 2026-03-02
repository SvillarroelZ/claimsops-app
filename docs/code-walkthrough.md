# Code Walkthrough: POST /api/claims Request Flow

**Objetivo:** Explicar paso a paso qué ocurre cuando se realiza un `POST /api/claims` para crear una nueva reclamación.

---

## Flujo General

```
┌─────────────┐
│ Client      │
│ POST Request│
└──────┬──────┘
       │
       ├── CreateClaimRequest (JSON)
       │   { memberId, amount, currency }
       │
       ▼
┌──────────────────────────────────────────┐
│ ClaimsController.CreateClaim()           │
│ [services/claims-service/Controllers/    │
│  ClaimsController.cs]                    │
│                                          │
│ 1. Log request                           │
│ 2. Call IClaimService.CreateClaimAsync() │
└────────────────┬─────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────┐
│ ClaimService.CreateClaimAsync()          │
│ [services/claims-service/Services/       │
│  ClaimService.cs]                        │
│                                          │
│ 1. Map DTO to Claim entity               │
│ 2. Set defaults (Id, Status=Draft)       │
│ 3. Call IClaimRepository.CreateAsync()   │
│ 4. Call RecordAuditEventAsync()          │
│ 5. Map Claim to ClaimResponse DTO        │
└────────────────┬─────────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
   ┌─────────┐    ┌──────────────┐
   │Database │    │AuditService  │
   │(Postgres│    │HTTP (async)  │
   │ )       │    └──────────────┘
   └─────────┘
        │
        │ Claim persisted
        ▼
┌──────────────────────────────────────────┐
│ ClaimsController returns 201 Created     │
│ + Location header + ClaimResponse body   │
└──────────────────────────────────────────┘
```

---

## Paso 1: Cliente envía solicitud HTTP

**Solicitud (HTTP):**
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

**Archivo de clase:** [ClaimsController.cs](../services/claims-service/Controllers/ClaimsController.cs)

---

## Paso 2: ClaimsController.CreateClaim() (Líneas 56-75)

**Ubicación:**
- Archivo: `services/claims-service/Controllers/ClaimsController.cs`
- Clase: `ClaimsController`
- Método: `CreateClaim(CreateClaimRequest request)`

**Qué hace:**

```csharp
[HttpPost]
[ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status201Created)]
public async Task<ActionResult<ClaimResponse>> CreateClaim([FromBody] CreateClaimRequest request)
{
    // 1. ASP.NET Core valida automáticamente el JSON contra CreateClaimRequest
    //    Si falla: retorna 400 BadRequest
    
    // 2. Registra el evento en logs
    _logger.LogInformation("POST /api/claims - Creating claim for member: {MemberId}", request.MemberId);
    
    // 3. Delega a la capa de servicios
    var claim = await _claimService.CreateClaimAsync(request);
    
    // 4. Retorna 201 Created con Location header y el body
    return CreatedAtAction(
        nameof(GetClaimById),           // Nombre del método para generar Location header
        new { id = claim.Id },          // Route values: /api/claims/{id}
        claim                           // Response body (objeto ClaimResponse)
    );
}
```

**Validaciones que ocurren aquí:**
- `MemberId` es requerido y debe tener 1-50 caracteres
- `Amount` debe ser mayor que 0
- `Currency` es opcional (por defecto: "USD")

**Respuesta esperada:**
```http
HTTP/1.1 201 Created
Location: /api/claims/550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "memberId": "MBR-12345",
  "amount": 500.00,
  "currency": "USD",
  "status": "Draft",
  "createdAt": "2026-03-02T05:30:00Z"
}
```

---

## Paso 3: ClaimService.CreateClaimAsync() (Líneas 86-116)

**Ubicación:**
- Archivo: `services/claims-service/Services/ClaimService.cs`
- Clase: `ClaimService`
- Método: `CreateClaimAsync(CreateClaimRequest request)`

**Qué hace:**

### 3.1 Mapear DTO a Entidad

El servicio transforma el `CreateClaimRequest` en un objeto `Claim` (entidad de dominio):

```csharp
var claim = new Claim
{
    Id = Guid.NewGuid(),           // Genera un UUID único
    MemberId = request.MemberId,   // Copia del request
    Amount = request.Amount,        // Copia del request
    Currency = request.Currency,    // Copia del request
    Status = ClaimStatus.Draft,    // Valor por defecto: Draft
    CreatedAt = DateTime.UtcNow    // Timestamp actual (UTC)
};
```

**Archivo de modelo:** [services/claims-service/Models/Claim.cs](../services/claims-service/Models/Claim.cs)

**Enum ClaimStatus:**
```csharp
public enum ClaimStatus
{
    Draft = 0,           // Reclamación nueva, sin procesar
    Submitted = 1,       // Enviada para revisión
    Approved = 2,        // Aprobada
    Rejected = 3         // Rechazada
}
```

### 3.2 Persistir en Base de Datos

```csharp
var created = await _repository.CreateAsync(claim);
```

Esto delega al repositorio (siguiente paso).

### 3.3 Registrar Evento de Auditoría

```csharp
await RecordAuditEventAsync(
    created.Id,
    "created",
    "system",
    $"Claim created for member {created.MemberId}"
);
```

Esto llama de forma **no-bloqueante** (async) al servicio de auditoría (ver Paso 4).

### 3.4 Mapear Entidad a DTO

```csharp
return MapToResponse(created);
```

Transforma la entidad `Claim` en `ClaimResponse` para enviar al cliente.

---

## Paso 4: ClaimRepository.CreateAsync() (Líneas 73-88)

**Ubicación:**
- Archivo: `services/claims-service/Repositories/ClaimRepository.cs`
- Clase: `ClaimRepository`
- Método: `CreateAsync(Claim claim)`

**Qué hace:**

```csharp
public async Task<Claim> CreateAsync(Claim claim)
{
    _logger.LogInformation("Creating claim for member: {MemberId} with amount: {Amount}", 
        claim.MemberId, claim.Amount);
    
    // 1. Agrega la entidad al DbContext (Entity Framework Core)
    _context.Claims.Add(claim);
    
    // 2. Guarda cambios en la base de datos PostgreSQL
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Claim created in database: {ClaimId}", claim.Id);
    
    // 3. Retorna la entidad (con valores generados confirmados)
    return claim;
}
```

**Base de datos:**
- **Tabla:** `Claims` (creada automáticamente por migrations)
- **Conexión:** PostgreSQL 15
- **Conexión string:** `Host=postgres;Port=5432;Database=claimsops_db;...`

**Resultado en BD:**
```sql
INSERT INTO "Claims" ("Id", "MemberId", "Amount", "Currency", "Status", "CreatedAt")
VALUES (
    '550e8400-e29b-41d4-a716-446655440000'::uuid,
    'MBR-12345',
    500.00,
    'USD',
    0,           -- ClaimStatus.Draft
    '2026-03-02 05:30:00'::timestamp with time zone
);
```

**Archivo de contexto:** [services/claims-service/Data/ClaimsDbContext.cs](../services/claims-service/Data/ClaimsDbContext.cs)

---

## Paso 5: RecordAuditEventAsync() (Líneas 132-165)

**Ubicación:**
- Archivo: `services/claims-service/Services/ClaimService.cs`
- Clase: `ClaimService` (método privado)
- Método: `RecordAuditEventAsync(...)`

**Qué hace:**

Este paso ocurre **en paralelo** con la persistencia en BD (es `async` y no se espera bloqueante).

### 5.1 Obtener URL del Audit Service

```csharp
var auditServiceUrl = _configuration["AuditService:BaseUrl"];
// Valor: "http://audit-service:8000" (desde docker-compose)
```

**Configuración (docker/docker-compose.yml):**
```yaml
claims-service:
  environment:
    AuditService__BaseUrl: "http://audit-service:8000"
```

### 5.2 Construir evento de auditoría

```csharp
var auditEvent = new
{
    claim_id = claimId.ToString(),
    event_type = "created",
    user_id = "system",
    details = "Claim created for member MBR-12345"
};
```

### 5.3 Hacer llamada HTTP POST al Audit Service

```csharp
var client = _httpClientFactory.CreateClient();
var json = JsonSerializer.Serialize(auditEvent);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync(
    $"{auditServiceUrl}/audit",
    content
);
```

**Solicitud HTTP enviada:**
```http
POST http://audit-service:8000/audit HTTP/1.1
Content-Type: application/json

{
  "claim_id": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "created",
  "user_id": "system",
  "details": "Claim created for member MBR-12345"
}
```

### 5.4 Manejo de errores

```csharp
try
{
    // ... HTTP call ...
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning("Audit event recording failed with status {StatusCode}", response.StatusCode);
    }
}
catch (Exception ex)
{
    // Si falla, solo registra el error pero NO interrumpe la creación de reclamación
    _logger.LogWarning(ex, "Failed to record audit event for claim {ClaimId}", claimId);
}
```

**Punto importante:** Si el audit-service está caído, la **reclamación se crea igualmente**. El audit es no-blocking.

---

## Paso 6: Audit Service recibe el evento

**Ubicación:**
- Archivo: `services/audit-service/main.py`
- Endpoint: `POST /audit`

**Qué hace:**

```python
@app.post("/audit")
async def record_audit_event(event: dict):
    """Record audit event in in-memory storage"""
    timestamp = datetime.utcnow().isoformat()
    
    audit_record = {
        "id": str(uuid4()),
        "claim_id": event.get("claim_id"),
        "event_type": event.get("event_type"),
        "user_id": event.get("user_id"),
        "details": event.get("details"),
        "timestamp": timestamp
    }
    
    # Almacenar en memoria (MVP - sin persistencia en BD)
    audit_events.append(audit_record)
    
    return {"id": audit_record["id"], "status": "recorded"}, 201
```

**Almacenamiento:**
- Global list en memoria: `audit_events = []`
- Se pierden si el contenedor se reinicia

**Respuesta:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": "7e3c2a9f-8b1d-4e6c-9a2b-5f1c8e9d7a3b",
  "status": "recorded"
}
```

---

## Resumen del Flujo Completo

| Paso | Componente | Acción | Tiempo Aproximado |
|------|-----------|--------|------------------|
| 1 | Cliente | Envía POST request | 0ms |
| 2 | ClaimsController | Valida y delega | 1ms |
| 3 | ClaimService | Mapea DTO a entidad | 2ms |
| 4 | ClaimRepository | Persiste en PostgreSQL | 10-20ms |
| 5 | ClaimService | Inicia llamada a audit-service (async) | 1ms |
| 6 | ClaimsController | Retorna 201 Created | 1ms |
| **Total (bloqueante)** | | | **~15-30ms** |
| 7 | AuditService | Recibe evento (simultáneamente) | 5-10ms |

**Nota:** La llamada al audit-service (paso 5-7) es **no-bloqueante**. El cliente recibe la respuesta 201 sin esperar a que el audit-service confirme.

---

## Verificación en Tiempo Real

### Ver logs del claims-service

```bash
docker compose -f docker/docker-compose.yml logs -f claims-service
```

**Salida esperada:**
```
claims-service | info: ClaimsService.Controllers.ClaimsController[0]
claims-service |       POST /api/claims - Creating claim for member: MBR-12345
claims-service | info: ClaimsService.Services.ClaimService[0]
claims-service |       Creating claim for member: MBR-12345, amount: 500 USD
claims-service | info: ClaimsService.Repositories.ClaimRepository[0]
claims-service |       Creating claim for member: MBR-12345 with amount: 500
claims-service | info: ClaimsService.Repositories.ClaimRepository[0]
claims-service |       Claim created in database: 550e8400-e29b-41d4-a716-446655440000
claims-service | info: ClaimsService.Services.ClaimService[0]
claims-service |       Claim created successfully: 550e8400-e29b-41d4-a716-446655440000
```

### Ver logs del audit-service

```bash
docker compose -f docker/docker-compose.yml logs -f audit-service
```

**Salida esperada:**
```
audit-service | INFO:     127.0.0.1:52380 - "POST /audit HTTP/1.1" 201 Created
```

### Verificar en base de datos

```bash
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db -c "SELECT * FROM \"Claims\";"
```

**Salida:**
```
                   id                  | memberId  | amount | currency | status |          createdAt
--------------------------------------+-----------+--------+----------+--------+-------------------------------
 550e8400-e29b-41d4-a716-446655440000 | MBR-12345 | 500.00 |   USD    |      0 | 2026-03-02 05:30:00+00:00
```

### Verificar eventos de auditoría

```bash
curl -s http://localhost:8000/audit | jq .
```

**Salida:**
```json
[
  {
    "id": "7e3c2a9f-8b1d-4e6c-9a2b-5f1c8e9d7a3b",
    "claim_id": "550e8400-e29b-41d4-a716-446655440000",
    "event_type": "created",
    "user_id": "system",
    "details": "Claim created for member MBR-12345",
    "timestamp": "2026-03-02T05:30:00.123456"
  }
]
```

---

## Manejo de Errores

### Escenario 1: JSON inválido

**Solicitud:**
```json
{
  "memberId": "MBR-12345",
  "amount": -100  // ❌ Negativoxbad
}
```

**Respuesta:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "errors": {
    "Amount": ["The Amount field must be greater than 0."]
  }
}
```

**Dónde ocurre:** ClaimsController, validación automática de DTOs

### Escenario 2: MemberId faltante

**Solicitud:**
```json
{
  "amount": 100
}
```

**Respuesta:**
```http
HTTP/1.1 400 Bad Request

{
  "errors": {
    "MemberId": ["Member ID is required"]
  }
}
```

**Dónde ocurre:** ClaimsController, data annotations en CreateClaimRequest

### Escenario 3: Error de base de datos

Si PostgreSQL está caído:

**Respuesta:**
```http
HTTP/1.1 500 Internal Server Error

{
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

**Logs:**
```
claims-service | warn: Microsoft.EntityFrameworkCore.DbUpdateException
claims-service |       Unable to connect to server
```

### Escenario 4: Audit service caído

La reclamación **se crea igualmente**:

**Respuesta:**
```http
HTTP/1.1 201 Created  ✅

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  ...
}
```

**Logs:**
```
claims-service | warn: Failed to record audit event for claim 550e8400-e29b-41d4-a716-446655440000
claims-service |       System.Net.Http.HttpRequestException: Connection refused...
```

---

## Archivos Clave Involucrados

```
services/claims-service/
├── Controllers/
│   └── ClaimsController.cs           ← Paso 2: Maneja solicitud HTTP
├── Services/
│   ├── IClaimService.cs              (interfaz)
│   └── ClaimService.cs               ← Paso 3: Lógica de negocio + Paso 5: Auditoría
├── Repositories/
│   ├── IClaimRepository.cs           (interfaz)
│   └── ClaimRepository.cs            ← Paso 4: Persistencia
├── Data/
│   └── ClaimsDbContext.cs            ← Paso 4: EF Core context
├── Models/
│   ├── Claim.cs                      ← Paso 3: Entidad de dominio
│   └── ClaimStatus.cs                ← Enum de estados
├── DTOs/
│   ├── CreateClaimRequest.cs         ← Paso 2: Request contract
│   └── ClaimResponse.cs              ← Paso 3: Response contract
└── Program.cs                        ← Inyección de dependencias

services/audit-service/
└── main.py                           ← Paso 6: Grabación de eventos
```

---

## Conceptos Clave

### Layered Architecture (Arquitectura de Capas)

```
┌─────────────────────────────────────┐
│ Controllers (HTTP)                  │  ← Interface externa
├─────────────────────────────────────┤
│ Services (Business Logic)           │  ← Lógica de negocio
├─────────────────────────────────────┤
│ Repositories (Data Access)          │  ← Abstracción de datos
├─────────────────────────────────────┤
│ Database (PostgreSQL)               │  ← Persistencia
└─────────────────────────────────────┘
```

**Ventajas:**
- Separación de responsabilidades
- Fácil de testear (mockear dependencies)
- Fácil de mantener y extender

### DTOs (Data Transfer Objects)

- **CreateClaimRequest**: Define qué envía el cliente
- **ClaimResponse**: Define qué envía el servidor
- **Claim (Model)**: Entidad interna de dominio

Esto permite cambiar la BD sin afectar la API pública.

### Async/Await

```csharp
await _claimService.CreateClaimAsync(request);
await _repository.CreateAsync(claim);
await client.PostAsync(...);  // No-blocking
```

Permite que el servidor maneje múltiples solicitudes de forma eficiente sin bloqueos.

---

**Fin del Walkthrough**
