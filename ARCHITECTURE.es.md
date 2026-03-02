# Documentación de Arquitectura ClaimsOps

**Versión:** 1.0.0  
**Última actualización:** 2 de marzo de 2026

## Tabla de Contenidos

1. [Descripción General del Sistema](#descripción-general-del-sistema)
2. [Stack Tecnológico](#stack-tecnológico)
3. [Decisiones de Arquitectura](#decisiones-de-arquitectura)
4. [Ciclo de Vida de Requests](#ciclo-de-vida-de-requests)
5. [Flujo de Datos](#flujo-de-datos)
6. [Infraestructura](#infraestructura)
7. [Seguridad](#seguridad)
8. [Guía de Desarrollo](#guía-de-desarrollo)
9. [Operaciones](#operaciones)

---

## Descripción General del Sistema

ClaimsOps es un sistema de gestión de reclamos de seguros basado en microservicios que demuestra patrones enterprise modernos.

### Componentes

```text
┌─────────────────────────────────────────────────────────────┐
│                     Capa de Cliente                          │
│  (Navegador, App Móvil, Consumidor API)                     │
└──────────────────┬──────────────────────────────────────────┘
                   │ HTTP/JSON
                   ▼
┌─────────────────────────────────────────────────────────────┐
│                Claims Service (C# .NET)                      │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐  │
│  │ Controllers  │──│   Services   │──│  Repositories   │  │
│  │  (API HTTP)  │  │  (Negocio)   │  │(Acceso a Datos) │  │
│  └──────────────┘  └──────────────┘  └────────┬────────┘  │
│                                                 │            │
└─────────────────────────────────────────────────┼──────────┘
                   │                              │
                   │ HTTP (Best-effort)          │ EF Core
                   ▼                              ▼
         ┌──────────────────┐         ┌──────────────────┐
         │  Audit Service   │         │   PostgreSQL     │
         │ (Python FastAPI) │         │  (Base de Datos) │
         │  - En memoria    │         │  - Tabla claims  │
         │  - Log eventos   │         │  - Migraciones   │
         └──────────────────┘         └──────────────────┘
```

### Características Clave

- **Microservicios:** Dos servicios independientes con stacks tecnológicos diferentes
- **API RESTful:** Comunicación estándar HTTP/JSON
- **Políglota:** Demuestra integración .NET + Python
- **Auditoría best-effort:** Registro de eventos no bloqueante
- **Nativo en contenedores:** Todos los servicios corren en Docker

---

## Stack Tecnológico

### Servicios Backend

#### Claims Service (C# .NET 10.0)

**Propósito:** API primaria para operaciones de reclamos (crear, leer, actualizar, eliminar)

**¿Por qué .NET?**
- **Seguridad de tipos:** El tipado fuerte reduce errores en runtime en producción
- **Rendimiento:** Optimizado para cargas de trabajo de alto throughput
- **Madurez enterprise:** Probado en industrias reguladas (finanzas, salud, seguros)
- **Ecosistema rico:** Autenticación, logging, ORM, frameworks de testing
- **Validación en tiempo de compilación:** Muchos bugs detectados antes del deploy

**Tecnologías clave:**
- ASP.NET Core 10.0 - Framework web
- Entity Framework Core 10.0 - ORM con migraciones
- Npgsql - Driver PostgreSQL
- Puerto: 5115

#### Audit Service (Python 3.11 + FastAPI)

**Propósito:** Servicio liviano de registro de eventos para trazabilidad operacional

**¿Por qué Python + FastAPI?**
- **Desarrollo rápido:** MVP construido en menos de 1 hora
- **Mínimo boilerplate:** Menos código comparado con Java/C# para servicios simples
- **Soporte async:** Basado en ASGI para alta concurrencia
- **Documentación automática:** OpenAPI/Swagger generado automáticamente
- **Demostración políglota:** Muestra independencia de servicios

**Tecnologías clave:**
- FastAPI - Framework web async moderno
- Pydantic - Validación de datos
- Uvicorn - Servidor ASGI
- Puerto: 8000

### Capa de Datos

#### PostgreSQL 15

**¿Por qué PostgreSQL?**
- **Cumplimiento ACID:** Requerido para integridad de transacciones financieras
- **Soporte JSON:** Flexibilidad híbrida relacional + documento
- **Código abierto:** Sin costos de licenciamiento
- **Escalabilidad:** Maneja millones de filas eficientemente
- **Comunidad:** Gran ecosistema de herramientas y extensiones

**Gestión de esquema:**
- Migraciones Entity Framework Core (code-first)
- Cambios de esquema versionados
- Aplicación automática al iniciar

### Infraestructura

#### Docker Compose

**Propósito:** Orquestación multi-contenedor para desarrollo local

**Beneficios:**
- **Reproducibilidad:** Entornos idénticos para todos los desarrolladores
- **Dependencias de servicios:** Orden de inicio automático con health checks
- **Aislamiento:** Cada servicio tiene dependencias independientes
- **Reset fácil:** `docker compose down -v` para estado limpio

**Red:**
- Red bridge `claimsops-network`
- Resolución DNS servicio-a-servicio
- Comunicación interna (postgres, claims-service, audit-service)

---

## Decisiones de Arquitectura

### Arquitectura en Capas (Claims Service)

```text
Controllers/     → Límite HTTP (routing, validación)
    ↓
Services/        → Lógica de negocio (orquestación, reglas)
    ↓
Repositories/    → Acceso a datos (abstracción ORM)
    ↓
EF Core          → Driver base de datos
    ↓
PostgreSQL       → Almacenamiento persistente
```

**¿Por qué este patrón?**
- **Separación de concerns:** Cada capa tiene responsabilidad única
- **Testabilidad:** Se pueden mockear dependencias para unit tests
- **Mantenibilidad:** Cambios aislados a capas específicas
- **Práctica estándar:** Patrón bien entendido en enterprise

### DTOs vs Modelos de Dominio

**DTOs (Data Transfer Objects):**
- `CreateClaimRequest` - Contrato de entrada API
- `ClaimResponse` - Contrato de salida API

**Modelo de Dominio:**
- `Claim` - Representación interna de entidad

**¿Por qué separados?**
- **Estabilidad API:** Puedes cambiar modelo interno sin romper clientes
- **Límite de validación:** DTOs enforzan restricciones externas
- **Seguridad:** No expones campos internos (audit trails, soft deletes)
- **Flexibilidad:** Múltiples DTOs pueden mapear a un modelo de dominio

### Patrón Best-Effort Audit

```csharp
// Claims service no falla si audit falla
try {
    await _httpClient.PostAsync("/audit", content);
} catch (Exception ex) {
    _logger.LogWarning("Audit falló: {Message}", ex.Message);
    // Continúa igual - el claim se guardó exitosamente
}
```

**¿Por qué este enfoque?**
- **Resiliencia:** Operación primaria exitosa incluso si audit service está caído
- **Ruta no crítica:** Audit es observabilidad, no requisito de negocio
- **Degradación elegante:** Sistema permanece operacional durante fallas parciales

**Trade-offs:**
- **Consistencia eventual:** Eventos de audit pueden retrasarse o perderse
- **Sin entrega garantizada:** Para audit crítico, usar cola de mensajes (Kafka, RabbitMQ)

---

## Ciclo de Vida de Requests

### Flujo POST /api/claims

Recorrido completo de creación de un nuevo reclamo:

```text
Cliente
  │
  │ 1. POST /api/claims { memberId, amount, currency }
  ▼
┌─────────────────────────────────────────────┐
│ ClaimsController.CreateClaim                │
│ - Recibe JSON (ASP.NET model binding)       │
│ - Valida DTO (data annotations)             │
│ - Delega a capa de servicio                 │
└──────────────────┬──────────────────────────┘
                   │ 2. CreateClaimAsync(request)
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimService.CreateClaimAsync               │
│ - Mapea DTO → Entidad dominio               │
│ - Asigna defaults:                          │
│   • Id = Guid.NewGuid()                     │
│   • Status = Draft                          │
│   • CreatedAt = DateTime.UtcNow             │
│ - Llama repositorio                         │
└──────────────────┬──────────────────────────┘
                   │ 3. CreateAsync(claim)
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimRepository.CreateAsync                 │
│ - DbContext.Claims.Add(claim)               │
│ - SaveChangesAsync()                        │
│ - Retorna entidad persistida con ID         │
└──────────────────┬──────────────────────────┘
                   │ 4. INSERT INTO claims (...)
                   ▼
             ┌──────────┐
             │PostgreSQL│ ← Fuente de verdad
             └──────────┘
                   │
                   │ 5. Audit best-effort
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimService.RecordAuditEventAsync          │
│ - HTTP POST a audit-service:8000/audit      │
│ - No bloqueante (try-catch)                 │
│ - Loguea warning en caso de falla           │
└──────────────────┬──────────────────────────┘
                   │ 6. POST /audit { claimId, action }
                   ▼
             ┌────────────┐
             │Audit Service│ ← En memoria (MVP)
             └────────────┘
                   │
                   │ 7. Respuesta 201 Created
                   ▼
┌─────────────────────────────────────────────┐
│ ClaimsController retorna                    │
│ - Status: 201 Created                       │
│ - Location: /api/claims/{id}                │
│ - Body: ClaimResponse DTO                   │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
                Cliente
```

### Transformación de Datos

| Etapa | Formato | Ejemplo | Propósito |
| --- | --- | --- | --- |
| **1. Request** | JSON | `{"memberId":"MBR-001","amount":100.0}` | Contrato API |
| **2. DTO** | Objeto C# | `CreateClaimRequest` | Límite validación |
| **3. Dominio** | Entidad | `Claim` (con Id, Status, CreatedAt) | Lógica negocio |
| **4. Persistencia** | Fila SQL | `INSERT INTO claims (...)` | Almacenamiento durable |
| **5. Response** | DTO | `ClaimResponse` | Contrato API |

### Reglas de Mapeo de Campos

| Campo | Origen | Default | Validación |
| --- | --- | --- | --- |
| `Id` | Servicio | `Guid.NewGuid()` | Siempre generado por sistema |
| `MemberId` | Request | Ninguno | Requerido, 1-50 chars |
| `Amount` | Request | Ninguno | Requerido, > 0 |
| `Currency` | Request | "USD" | Opcional, 3-char ISO |
| `Status` | Servicio | `Draft` | Enum: Draft, Submitted, Approved, Rejected |
| `CreatedAt` | Servicio | `DateTime.UtcNow` | Siempre UTC |

---

## Flujo de Datos

### Esquema Base de Datos

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

### Mapeo Entity Framework

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

### Migraciones

**Crear migración:**
```bash
cd services/claims-service
dotnet ef migrations add NombreDescriptivo
```

**Aplicar migración:**
```bash
# Manual
dotnet ef database update

# Automático (al iniciar contenedor)
# Program.cs contiene: dbContext.Database.Migrate()
```

**Ubicación archivos migración:**
- `services/claims-service/Migrations/`
- Versionados (parte del historial git)
- Aplicados automáticamente cuando contenedor inicia

---

## Infraestructura

### Configuración Docker Compose

**Servicios:**

| Servicio | Imagen | Puerto | Dependencias | Propósito |
| --- | --- | --- | --- | --- |
| postgres | postgres:15 | 5432 | - | Base de datos |
| claims-service | Custom (.NET) | 5115 | postgres | API primaria |
| audit-service | Custom (Python) | 8000 | - | Registro eventos |

**Red:**
- Tipo: Bridge
- Nombre: `claimsops-network`
- DNS: Resolución automática de servicios (ej. `http://postgres:5432`)

**Volúmenes:**
- `postgres_data` → `/var/lib/postgresql/data` (almacenamiento persistente)

**Health checks:**
```yaml
postgres:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U claimsops_user"]
    interval: 10s
    timeout: 5s
    retries: 5
```

### Variables de Entorno

**Variables requeridas** (definir en `docker/.env`):
```bash
# Base de datos
POSTGRES_USER=claimsops_user
POSTGRES_PASSWORD=password_seguro_aqui
POSTGRES_DB=claimsops_db

# Connection strings (auto-construido desde arriba)
CONNECTION_STRING=Host=postgres;Database=claimsops_db;Username=claimsops_user;Password=password_seguro_aqui

# URLs de servicios
AUDIT_SERVICE_URL=http://audit-service:8000
```

**Seguridad:** Nunca commitear archivo `docker/.env` (listado en `.gitignore`)

### Comunicación entre Servicios

**Claims Service → PostgreSQL:**
- Protocolo: Protocolo wire PostgreSQL
- Conexión: `Host=postgres;Port=5432;...`
- Driver: Npgsql via Entity Framework Core

**Claims Service → Audit Service:**
- Protocolo: HTTP/JSON
- URL: `http://audit-service:8000/audit`
- Método: POST (no bloqueante)

---

## Seguridad

### Gestión de Secretos

#### Desarrollo Local

**Estructura archivos:**
```
docker/
├── .env              ← Contiene secretos (NUNCA COMMITEAR)
├── .env.example      ← Template (SEGURO PARA COMMIT)
└── docker-compose.yml
```

**Generar password seguro:**
```bash
openssl rand -base64 24
# Copiar resultado a docker/.env
```

**Verificación:**
```bash
# Revisar que .env está ignorado
grep "\.env" .gitignore

# Verificar no está staged
git status | grep "\.env"  # Debe estar vacío
```

#### Producción

**Usar gestores de secretos apropiados:**

| Plataforma | Servicio | Patrón |
| --- | --- | --- |
| AWS | Secrets Manager | `arn:aws:secretsmanager:region:...` |
| Azure | Key Vault | `https://vault.azure.net/secrets/...` |
| GCP | Secret Manager | `projects/{id}/secrets/{name}` |
| Kubernetes | Secrets | Recursos ConfigMap + Secret |

**Nunca:**
- ❌ Hardcodear credenciales en código
- ❌ Commitear archivos `.env`
- ❌ Loguear datos sensibles
- ❌ Exponer secretos en mensajes de error

### Validación de Input

**Capas de validación:**

1. **Data Annotations DTO** (automático):
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

3. **Reglas de Negocio** (capa servicio):
```csharp
if (claim.Amount > MAX_CLAIM_AMOUNT)
    throw new BusinessException("Monto excede límite");
```

### Configuración CORS

**Actual** (`appsettings.json`):
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

**Producción:** Actualizar con dominios frontend reales

**GitHub Codespaces:** Requiere configuración de origen dinámico (ver README)

### Autenticación y Autorización

**Estado actual:** No implementado (MVP Fase 1)

**Roadmap (Fase 2):**
- Autenticación basada en tokens JWT
- Control de acceso basado en roles (RBAC)
- API keys servicio-a-servicio
- Integración OAuth2 / OpenID Connect

---

## Guía de Desarrollo

### Prerequisitos

- .NET 10.0 SDK
- Docker Desktop
- Python 3.11+ (para desarrollo local audit service)
- Git

### Setup Inicial

```bash
# Clonar repositorio
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app

# Configurar entorno
cd docker
cp .env.example .env
# Editar .env con passwords seguros

# Iniciar servicios
docker compose up -d --build

# Verificar
docker compose ps
curl http://localhost:5115/health
curl http://localhost:8000/health
```

### Desarrollo Local (Sin Docker)

**Terminal 1 - PostgreSQL:**
```bash
docker compose up -d postgres
```

**Terminal 2 - Claims Service:**
```bash
cd services/claims-service

# Actualizar connection string (usar localhost en vez de postgres)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=claimsops_db;..."

# Ejecutar
dotnet run

# API disponible en http://localhost:5115
```

**Terminal 3 - Audit Service:**
```bash
cd services/audit-service

python3 -m venv venv
source venv/bin/activate  # Linux/Mac
# venv\Scripts\activate   # Windows

pip install -r requirements.txt
uvicorn main:app --reload --port 8000

# API disponible en http://localhost:8000
```

### Crear Migraciones

```bash
cd services/claims-service

# Agregar migración
dotnet ef migrations add NombreMigracion

# Aplicar localmente
dotnet ef database update

# Deploy: migraciones se aplican auto al iniciar contenedor
```

### Agregar Nuevos Endpoints

1. **Definir DTO** (`DTOs/TuRequestDto.cs`)
2. **Agregar Método Repository** (`Repositories/ITuRepository.cs`)
3. **Implementar Repository** (`Repositories/TuRepository.cs`)
4. **Agregar Método Service** (`Services/ITuService.cs`)
5. **Implementar Service** (`Services/TuService.cs`)
6. **Agregar Action Controller** (`Controllers/TuController.cs`)
7. **Registrar DI** (si hay nuevas interfaces en `Program.cs`)
8. **Testear localmente** con `dotnet run`

### Testing

**Tests manuales:**
```bash
# Health checks
curl http://localhost:5115/health
curl http://localhost:8000/health

# Crear claim
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"MBR-TEST","amount":100.0,"currency":"USD"}'

# Listar claims
curl http://localhost:5115/api/claims

# Ver eventos audit
curl http://localhost:8000/audit
```

**Tests automatizados** (futuro):
- xUnit para unit tests C#
- pytest para tests Python
- Integration tests con test containers

---

## Operaciones

### Comandos Comunes

**Docker Compose:**
```bash
# Iniciar todos los servicios
docker compose -f docker/docker-compose.yml up -d

# Ver logs (todos)
docker compose -f docker/docker-compose.yml logs -f

# Ver logs (servicio específico)
docker compose -f docker/docker-compose.yml logs -f claims-service

# Revisar estado
docker compose -f docker/docker-compose.yml ps

# Reiniciar servicio
docker compose -f docker/docker-compose.yml restart claims-service

# Detener todos (mantener datos)
docker compose -f docker/docker-compose.yml stop

# Remover todos (mantener volúmenes)
docker compose -f docker/docker-compose.yml down

# Reset completo (borrar datos)
docker compose -f docker/docker-compose.yml down -v
```

**Acceso Base de Datos:**
```bash
# Conectar a PostgreSQL
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db

# Queries SQL
SELECT * FROM claims;
\dt   -- Listar tablas
\q    -- Salir
```

**Comandos .NET:**
```bash
cd services/claims-service

dotnet build                    # Compilar
dotnet run                      # Ejecutar localmente
dotnet test                     # Ejecutar tests
dotnet add package NombrePaquete  # Instalar paquete NuGet
```

### Solución de Problemas (Troubleshooting)

**Puerto ya en uso:**
```bash
# Encontrar proceso
lsof -i :5115
kill -9 <PID>

# O cambiar puerto en docker-compose.yml
ports:
  - "5116:5115"  # Externo:Interno
```

**Falla conexión base de datos:**
```bash
# Verificar PostgreSQL está corriendo
docker compose ps postgres

# Revisar health
docker compose logs postgres

# Resetear base de datos
docker compose down -v postgres
docker compose up -d postgres
sleep 10
docker compose up -d claims-service
```

**Migraciones no aplicadas:**
```bash
# Revisar logs
docker compose logs claims-service | grep migration

# Aplicación manual
cd services/claims-service
dotnet ef database update
```

**Claims service no puede alcanzar audit service:**
```bash
# Verificar red
docker network ls | grep claimsops

# Revisar DNS servicio
docker exec -it claimsops-claims-service ping audit-service

# Revisar variable entorno
docker exec -it claimsops-claims-service printenv AUDIT_SERVICE_URL
```

**Reset completo (clean slate):**
```bash
# Opción nuclear - remueve todo
docker compose down -v
docker system prune -a --volumes
docker compose up -d --build
```

### Monitoreo (Futuro)

**Roadmap Fase 4:**
- Prometheus para recolección de métricas
- Grafana para dashboards de visualización
- OpenTelemetry para tracing distribuido
- Logging estructurado con Serilog/ELK stack

---

## Recursos Adicionales

- [README.es.md](README.es.md) - Guía inicio rápido
- [CONTRIBUTING.md](CONTRIBUTING.md) - Guías desarrollo
- [GitHub Issues](https://github.com/SvillarroelZ/claimsops-app/issues) - Reportes bugs y features

---

**Documento mantenido por:** Equipo de Ingeniería  
**¿Preguntas?** Abre un issue en GitHub
