# ClaimsOps - Guía de Inicio Rápido

**Versión:** 1.0  
**Fecha:** Marzo 2026

---

## ESTRUCTURA DEL PROYECTO

```
claimsops-app/
├── .github/                    # CI/CD y plantillas de GitHub
│   ├── ISSUE_TEMPLATE/         # Templates para bugs y features
│   └── pull_request_template.md
├── docker/                     # Orquestación de contenedores
│   ├── docker-compose.yml      # Define 3 servicios (postgres, claims, audit)
│   ├── init.sql                # Script de inicialización DB
│   ├── .env                    # Variables de entorno (no versionado)
│   └── .env.example            # Template de configuración
├── services/                   # Microservicios (arquitectura modular)
│   ├── claims-service/         # API principal (.NET 10)
│   │   ├── Controllers/        # HTTP endpoints (ClaimsController, HealthController)
│   │   ├── Services/           # Lógica de negocio (ClaimService)
│   │   ├── Repositories/       # Acceso a datos (ClaimRepository)
│   │   ├── Models/             # Entidades de dominio (Claim, ClaimStatus)
│   │   ├── DTOs/               # Contratos API (CreateClaimRequest, ClaimResponse)
│   │   ├── Data/               # DbContext de EF Core
│   │   ├── Migrations/         # Versionado de esquema DB
│   │   └── Program.cs          # Bootstrap de la aplicación
│   └── audit-service/          # API de auditoría (Python FastAPI)
│       ├── main.py             # Aplicación completa FastAPI
│       ├── requirements.txt    # Dependencias Python
│       └── Dockerfile
├── tests/                      # Suite de pruebas automatizadas
│   └── smoke-tests.sh          # Tests de integración end-to-end
├── ARCHITECTURE.md             # Documentación técnica completa (EN)
├── ARCHITECTURE.es.md          # Documentación técnica completa (ES)
├── README.md                   # Guía rápida de inicio (EN)
├── README.es.md                # Guía rápida de inicio (ES)
├── CONTRIBUTING.md             # Guía de contribución
└── claimsops-app.sln           # Visual Studio solution file
```

---

## ARQUITECTURA DE CLAIMS SERVICE (Capas)

```
HTTP Request
    ↓
Controllers/                # Capa de presentación
    ├── ClaimsController.cs     # POST /api/claims, GET /api/claims, GET /api/claims/{id}
    └── HealthController.cs     # GET /health
    ↓
Services/                   # Capa de lógica de negocio
    └── ClaimService.cs         # Validación, orquestación, llamada a audit
    ↓
Repositories/               # Capa de acceso a datos
    └── ClaimRepository.cs      # Queries EF Core
    ↓
Data/                       # Capa de persistencia
    └── ClaimsDbContext.cs      # DbContext (ORM)
    ↓
PostgreSQL Database
```

---

## ORDEN DE COMANDOS PARA PROBAR LA APP

### PASO 1: INICIAR EL SISTEMA

```bash
# Ubicarte en la carpeta docker
cd docker

# Iniciar todos los servicios (PostgreSQL, Claims Service, Audit Service)
docker compose up -d --build

# Esperar 15 segundos a que los servicios inicien
sleep 15

# Verificar que todo esté corriendo
docker compose ps
```

**Resultado esperado:**
```
NAME                         STATUS
claimsops-postgres           Up (healthy)
claimsops-claims-service     Up
claimsops-audit-service      Up
```

---

### PASO 2: VERIFICAR SALUD DE LOS SERVICIOS

```bash
# Health check del Claims Service (puerto 5115)
curl http://localhost:5115/health

# Health check del Audit Service (puerto 8000)
curl http://localhost:8000/health
```

**Resultado esperado:**
```json
{"status":"healthy","service":"claims-service","timestamp":"2026-03-02T..."}
{"status":"healthy","service":"audit-service","timestamp":"2026-03-02T..."}
```

---

### PASO 3: EJECUTAR TESTS AUTOMÁTICOS

```bash
# Volver a la raíz del proyecto
cd ..

# Ejecutar la suite de tests (13 tests)
bash tests/smoke-tests.sh
```

**Resultado esperado:**
```
========================================
Test Summary
========================================
Total tests run: 13
Passed: 13
Failed: 0

[SUCCESS] All tests passed!
```

---

### PASO 4: PRUEBAS MANUALES

#### Crear un claim nuevo

```bash
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{
    "memberId": "MANUAL-TEST-001",
    "amount": 250.75,
    "currency": "USD"
  }'
```

**Resultado esperado:**
```json
{
  "id": "9c312731-e036-49fc-9c92-55629b759d59",
  "memberId": "MANUAL-TEST-001",
  "status": "Draft",
  "amount": 250.75,
  "currency": "USD",
  "createdAt": "2026-03-02T12:00:00Z"
}
```

**IMPORTANTE:** Guarda el `id` que recibes para los siguientes comandos.

---

#### Ver todos los claims

```bash
curl http://localhost:5115/api/claims
```

**Resultado esperado:**
```json
[
  {
    "id": "9c312731-e036-49fc-9c92-55629b759d59",
    "memberId": "MANUAL-TEST-001",
    "status": "Draft",
    "amount": 250.75,
    "currency": "USD",
    "createdAt": "2026-03-02T12:00:00Z"
  }
]
```

---

#### Ver un claim específico

```bash
# Reemplaza <ID_DEL_CLAIM> con el id que guardaste
curl http://localhost:5115/api/claims/<ID_DEL_CLAIM>
```

**Ejemplo real:**
```bash
curl http://localhost:5115/api/claims/9c312731-e036-49fc-9c92-55629b759d59
```

---

#### Ver eventos de auditoría

```bash
# Ver TODOS los eventos
curl http://localhost:8000/audit

# Ver eventos de UN claim específico
curl "http://localhost:8000/audit?claim_id=<ID_DEL_CLAIM>"
```

**Resultado esperado:**
```json
[
  {
    "id": "8898cbb8-6919-4e27-ad34-143eee5dc3d5",
    "claim_id": "9c312731-e036-49fc-9c92-55629b759d59",
    "event_type": "created",
    "user_id": "system",
    "details": "Claim created for member MANUAL-TEST-001",
    "timestamp": "2026-03-02T12:00:01Z"
  }
]
```

---

### PASO 5: VER LOGS (Si algo falla)

```bash
# Volver a la carpeta docker
cd docker

# Ver logs de todos los servicios
docker compose logs

# Ver solo errores del Claims Service
docker compose logs claims-service | grep -i error

# Ver últimas 20 líneas del Audit Service
docker compose logs --tail=20 audit-service

# Seguir logs en tiempo real
docker compose logs -f

# Detener seguimiento: Ctrl+C
```

---

### PASO 6: CONECTARSE A LA BASE DE DATOS

```bash
# Conectar a PostgreSQL
docker exec -it claimsops-postgres psql -U admin -d claimsops_db
```

**Comandos dentro de psql:**
```sql
-- Ver todas las tablas
\dt

-- Ver estructura de la tabla Claims
\d "Claims"

-- Ver todos los claims
SELECT * FROM "Claims";

-- Contar claims
SELECT COUNT(*) FROM "Claims";

-- Ver migraciones aplicadas
SELECT * FROM "__EFMigrationsHistory";

-- Salir
\q
```

---

### PASO 7: DETENER TODO

```bash
cd docker

# Detener servicios pero mantener datos
docker compose down

# Detener servicios y BORRAR TODOS LOS DATOS (reset completo)
docker compose down -v
```

---

## COMANDOS RÁPIDOS (Todo en uno)

### Iniciar y probar todo

```bash
cd docker && docker compose up -d --build && sleep 15 && \
curl http://localhost:5115/health && \
curl http://localhost:8000/health && \
cd .. && bash tests/smoke-tests.sh
```

### Crear un claim de prueba

```bash
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"TEST-001","amount":100.50,"currency":"USD"}'
```

### Ver todos los claims

```bash
curl http://localhost:5115/api/claims
```

### Ver todos los eventos de auditoría

```bash
curl http://localhost:8000/audit
```

### Detener todo

```bash
cd docker && docker compose down
```

---

## ENDPOINTS DISPONIBLES

### Claims Service (puerto 5115)

| Método | Endpoint | Descripción | Body de ejemplo |
|--------|----------|-------------|-----------------|
| GET | `/health` | Health check | - |
| POST | `/api/claims` | Crear claim | `{"memberId":"MBR001","amount":500.75,"currency":"USD"}` |
| GET | `/api/claims` | Listar todos los claims | - |
| GET | `/api/claims/{id}` | Obtener claim específico | - |
| GET | `/openapi/v1.json` | Especificación OpenAPI | - |

### Audit Service (puerto 8000)

| Método | Endpoint | Descripción | Query Params |
|--------|----------|-------------|--------------|
| GET | `/health` | Health check | - |
| POST | `/audit` | Registrar evento | - |
| GET | `/audit` | Listar eventos | `?claim_id=<guid>` (opcional) |
| GET | `/docs` | Swagger UI interactivo | - |
| GET | `/openapi.json` | Especificación OpenAPI | - |

---

## URLS PARA EL NAVEGADOR

- `http://localhost:5115/health` - Health check del Claims Service
- `http://localhost:5115/api/claims` - Lista de claims (JSON)
- `http://localhost:8000/health` - Health check del Audit Service
- `http://localhost:8000/audit` - Lista de eventos de auditoría (JSON)
- `http://localhost:8000/docs` - **Swagger UI interactivo** (prueba la API con clicks)

---

## PRUEBAS DE VALIDACIÓN

### Crear claim con datos inválidos (debe fallar con 400)

```bash
# Amount negativo
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"TEST","amount":-100,"currency":"USD"}'

# Amount fuera de rango (>1,000,000)
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"TEST","amount":2000000,"currency":"USD"}'

# MemberId vacío
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"","amount":100,"currency":"USD"}'
```

**Todas deben retornar:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Amount": ["Amount must be between 0.01 and 1,000,000"],
    "MemberId": ["Member ID is required"]
  }
}
```

---

## PRUEBAS DE RESILIENCIA

### Simular fallo del audit service

```bash
# 1. Detener audit-service
docker stop claimsops-audit-service

# 2. Crear claim (debe funcionar sin audit)
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"RESILIENCE-TEST","amount":500,"currency":"USD"}'

# Debe retornar 201 Created (operación exitosa)

# 3. Reiniciar audit-service
docker start claimsops-audit-service
```

**Conclusión:** La arquitectura "best-effort" permite que la operación principal funcione incluso si el servicio de auditoría está caído.

---

## GESTIÓN DE BASE DE DATOS

### Aplicar nuevas migraciones (si modificas el modelo)

```bash
cd services/claims-service

# Crear migración
dotnet ef migrations add NombreDeLaMigracion

# Aplicar migración
dotnet ef database update

# Ver migraciones pendientes
dotnet ef migrations list

# Revertir última migración
dotnet ef database update NombreMigracionAnterior

# Eliminar última migración (si no se aplicó)
dotnet ef migrations remove
```

---

## TROUBLESHOOTING

### Los servicios no arrancan

```bash
# Ver logs completos
cd docker && docker compose logs

# Verificar que los puertos 5115 y 8000 estén libres
lsof -i :5115
lsof -i :8000

# Eliminar contenedores y volúmenes, reintentar
docker compose down -v
docker compose up -d --build
```

### Claims service retorna 500

```bash
# Ver logs del servicio
docker compose logs claims-service --tail=50

# Verificar conexión a PostgreSQL
docker exec -it claimsops-postgres pg_isready -U admin
```

### Base de datos vacía después de crear claims

```bash
# Verificar que el volumen persiste
docker volume ls | grep postgres

# Conectar y verificar datos
docker exec -it claimsops-postgres psql -U admin -d claimsops_db -c 'SELECT COUNT(*) FROM "Claims";'
```

### Tests fallan

```bash
# Verificar que los servicios estén corriendo
docker compose ps

# Esperar más tiempo antes de ejecutar tests
sleep 30 && bash tests/smoke-tests.sh

# Verificar logs de errores
docker compose logs | grep -i "error\|exception"
```

---

## VARIABLES DE ENTORNO

**Archivo:** `docker/.env`

```env
# PostgreSQL Configuration
POSTGRES_USER=admin
POSTGRES_PASSWORD=admin123
POSTGRES_DB=claimsops_db

# Claims Service Configuration
ConnectionStrings__DefaultConnection=Host=postgres;Database=claimsops_db;Username=admin;Password=admin123
AUDIT_SERVICE_URL=http://audit-service:8000

# Audit Service Configuration
# (No requiere configuración adicional para MVP)
```

**Nota:** Para producción, usar secretos seguros y no hardcodear credenciales.

---

## RESUMEN EJECUTIVO

**Para probar la app completa en 1 minuto:**

```bash
# 1. Iniciar
cd docker && docker compose up -d --build && sleep 15

# 2. Verificar salud
curl http://localhost:5115/health && curl http://localhost:8000/health

# 3. Ejecutar tests
cd .. && bash tests/smoke-tests.sh

# 4. Crear un claim
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"QUICK-TEST","amount":100,"currency":"USD"}'

# 5. Ver claims
curl http://localhost:5115/api/claims

# 6. Ver auditoría
curl http://localhost:8000/audit

# 7. Detener
cd docker && docker compose down
```

---

## DOCUMENTACIÓN ADICIONAL

- **Arquitectura completa:** Ver `ARCHITECTURE.md` o `ARCHITECTURE.es.md`
- **Guía de contribución:** Ver `CONTRIBUTING.md`
- **README del proyecto:** Ver `README.md` o `README.es.md`

---

**Última actualización:** 2 de Marzo, 2026  
**Autor:** Sofia Villarroel Z  
**Repositorio:** https://github.com/SvillarroelZ/claimsops-app
