# ClaimsOps Application

[**English**](README.md) | [**Español**](README.es.md)

Sistema de gestión de reclamaciones de seguros de nivel empresarial, construido con arquitectura de microservicios.

**Estado:** MVP Completo  
**Versión:** 1.0.0

## Inicio Rápido

### Opción 1: GitHub Codespaces (Recomendado)

1. Abre este repositorio en Codespaces
2. En la terminal, configura las variables de entorno:

   ```bash
   cd docker
   cp .env.example .env
   ```

3. Inicia todos los servicios:

   ```bash
   docker compose -f docker-compose.yml up -d --build
   ```

4. **Haz los puertos públicos** (requerido para acceso en navegador):
   - Presiona `Ctrl+Shift+P` (o `Cmd+Shift+P` en Mac)
   - Escribe: "Ports: Focus on Ports View"
   - Presiona Enter
   - Haz clic derecho en puerto **8000** → Port Visibility → **Public**
   - Haz clic derecho en puerto **5115** → Port Visibility → **Public**

5. **Accede a los servicios en tu navegador**:
   - Obtén el nombre de tu Codespace: ejecuta `echo $CODESPACE_NAME`
   - Reemplaza `YOUR-CODESPACE-NAME` con el nombre real
   - **Swagger UI (API Interactiva):** `https://YOUR-CODESPACE-NAME-8000.app.github.dev/docs`
   - **Claims API:** `https://YOUR-CODESPACE-NAME-5115.app.github.dev/api/claims`
   - **Health Check:** `https://YOUR-CODESPACE-NAME-5115.app.github.dev/health`

6. **Desde la terminal** (siempre funciona):

   ```bash
   # Verifica que los servicios estén corriendo
   curl http://localhost:5115/health | jq .
   curl http://localhost:8000/health | jq .
   
   # Crea una reclamación de prueba
   curl -X POST http://localhost:5115/api/claims \
     -H "Content-Type: application/json" \
     -d '{"memberId":"MBR-001","amount":250.50,"currency":"USD"}' | jq .
   
   # Lista todas las reclamaciones
   curl http://localhost:5115/api/claims | jq .
   ```

### Opción 2: Desarrollo Local (Mac / Linux)

```bash
# Clona el repositorio
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app

# Configura el entorno (IMPORTANTE: crea .env en la carpeta docker/)
cd docker
cp .env.example .env

# Inicia todos los servicios
docker compose -f docker-compose.yml up -d --build

# Verifica que los servicios estén corriendo
sleep 10
curl http://localhost:5115/health | jq .
curl http://localhost:8000/health | jq .

# Crea una reclamación de prueba
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{"memberId":"MBR-001","amount":250.50,"currency":"USD"}' | jq .

# Ver documentación Swagger
# Abre en navegador: http://localhost:8000/docs
```

## Arquitectura

```text
┌─────────────────────────────────────────────┐
│          Red Docker Compose                  │
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

## Stack Tecnológico

### Servicios Backend

| Componente | Tecnología | Puerto | Propósito |
| ----------- | ----------- | -------- | ----------- |
| **Claims Service** | C# .NET 10.0 | 5115 | API principal para gestión de reclamaciones |
| **Audit Service** | Python 3.11 + FastAPI | 8000 | Grabación de eventos de auditoría |
| **Database** | PostgreSQL 15 | 5432 | Persistencia de datos |

### Librerías y Herramientas Clave

- **Entity Framework Core 10.0** - ORM con migraciones code-first
- **Npgsql** - Driver de PostgreSQL para .NET
- **FastAPI** - Framework web asincrónico moderno para Python
- **Uvicorn** - Servidor ASGI para Python
- **Docker Compose** - Orquestación multi-contenedor

## Estructura del Proyecto

```text
claimsops-app/
├── services/
│   ├── claims-service/        # API Web C# .NET
│   │   ├── Controllers/       # Endpoints HTTP
│   │   ├── Services/          # Lógica de negocio
│   │   ├── Repositories/      # Acceso a datos (EF Core)
│   │   ├── Models/            # Entidades de dominio
│   │   ├── DTOs/              # Contratos API
│   │   ├── Data/              # DbContext
│   │   ├── Migrations/        # Migraciones EF Core
│   │   └── Dockerfile
│   │
│   └── audit-service/         # FastAPI Python
│       ├── main.py            # Aplicación y endpoints
│       ├── requirements.txt   # Dependencias Python
│       └── Dockerfile
│
├── docker/
│   ├── docker-compose.yml     # Orquestación de servicios
│   ├── init.sql               # Setup de PostgreSQL
│   └── README.md
│
├── docs/
│   ├── runbook.md             # Guía técnica completa
│   └── security.md            # Documentación de seguridad
│
├── .env.example               # Plantilla de entorno
├── .gitignore
└── README.md / README.es.md
```

## Endpoints API

### Claims Service (C# .NET)

| Método | Endpoint | Descripción | Respuesta |
| -------- | ---------- | ------------- | ---------- |
| GET | `/health` | Health check | 200 OK |
| POST | `/api/claims` | Crear nueva reclamación | 201 Created |
| GET | `/api/claims` | Listar todas las reclamaciones | 200 OK |
| GET | `/api/claims/{id}` | Obtener reclamación por ID | 200 OK / 404 Not Found |

### Audit Service (Python FastAPI)

| Método | Endpoint | Descripción | Respuesta |
| -------- | ---------- | ------------- | ---------- |
| GET | `/health` | Health check | 200 OK |
| POST | `/audit` | Grabar evento de auditoría | 201 Created |
| GET | `/audit` | Listar todos los eventos | 200 OK |
| GET | `/audit?claim_id={id}` | Filtrar por reclamación | 200 OK |

## Para Empezar

### Requisitos Previos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (para desarrollo local)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (requerido)
- [Python 3.11+](https://www.python.org/downloads/) (para desarrollo local)
- curl o [Postman](https://www.postman.com/) (para pruebas)

### Instalación y Configuración

**1. Clonar y Configurar** (tu máquina local o Codespace)

```bash
git clone https://github.com/SvillarroelZ/claimsops-app.git
cd claimsops-app
cd docker
cp .env.example .env
# IMPORTANTE: .env debe estar en carpeta docker/, no en raíz del repositorio
```

**2. Iniciar Todos los Servicios** (desde carpeta docker/)

```bash
# Sintaxis moderna (recomendada)
docker compose -f docker-compose.yml up -d --build

# O sintaxis antigua (sigue funcionando)
docker-compose up -d --build
```

### 3. Verificar Salud de Servicios

```bash
# Espera 10 segundos para que inicie
sleep 10

# Verifica claims-service
curl http://localhost:5115/health | jq .

# Verifica audit-service
curl http://localhost:8000/health | jq .
```

**4. Prueba el Flujo Completo** (Crear → Auditar → Listar)

```bash
# Crea una reclamación (persiste en PostgreSQL + genera evento de auditoría)
curl -X POST http://localhost:5115/api/claims \
  -H "Content-Type: application/json" \
  -d '{
    "memberId": "MBR-12345",
    "amount": 500.00,
    "currency": "USD"
  }' | jq .

# Lista todas las reclamaciones de base de datos
curl http://localhost:5115/api/claims | jq .

# Ve los eventos de auditoría grabados por audit-service
curl http://localhost:8000/audit | jq .
```

### 5. Detener Servicios

```bash
# Detiene servicios (mantiene datos)
cd docker
docker compose -f docker-compose.yml stop

# Detiene y elimina contenedores (datos persisten en volúmenes)
docker compose -f docker-compose.yml down

# Reset completo (elimina todos los datos y volúmenes)
docker compose -f docker-compose.yml down -v
```

## Flujo de Desarrollo

### Ejecutar Localmente (Sin Docker)

**Claims Service:**

```bash
cd services/claims-service

# Asegúrate que PostgreSQL esté corriendo
docker-compose up -d postgres

# Ejecuta la aplicación
dotnet run

# API disponible en http://localhost:5115
```

**Audit Service:**

```bash
cd services/audit-service

# Crea entorno virtual
python3 -m venv venv
source venv/bin/activate  # Linux/Mac
venv\Scripts\activate     # Windows

# Instala dependencias
pip install -r requirements.txt

# Ejecuta la aplicación
uvicorn main:app --reload --port 8000

# API disponible en http://localhost:8000
```

### Realizar Cambios a Base de Datos

```bash
cd services/claims-service

# 1. Modifica Models/Claim.cs
# 2. Crea migración
dotnet ef migrations add DescriptiveName

# 3. Aplica migración
dotnet ef database update

# 4. Reconstruye contenedor (auto-aplica migraciones al iniciar)
cd ../../docker
docker-compose up -d --build claims-service
```

### Agregar Nuevos Endpoints

1. Define DTO en `DTOs/`
2. Agrega método en repositorio en `Repositories/`
3. Agrega método en servicio en `Services/`
4. Agrega acción en controlador en `Controllers/`
5. Prueba localmente con `dotnet run`
6. Haz commit de cambios

## Documentación

- **[Runbook Técnico Completo](docs/runbook.md)** - Guía profunda con explicaciones tecnológicas
- **[Documentación de Seguridad](docs/security.md)** - Consideraciones de seguridad
- **[Guía Docker](docker/README.md)** - Detalles de orquestación de contenedores

## Comandos Comunes

```bash
# Docker Compose (sintaxis moderna - recomendada)
docker compose -f docker/docker-compose.yml up -d              # Inicia todos servicios
docker compose -f docker/docker-compose.yml logs -f            # Ver todos los logs
docker compose -f docker/docker-compose.yml logs -f claims-service  # Ver logs específicos
docker compose -f docker/docker-compose.yml ps                 # Ver estado de servicios
docker compose -f docker/docker-compose.yml restart claims-service  # Reinicia servicio
docker compose -f docker/docker-compose.yml down -v            # Reset completo

# Comandos .NET (desde services/claims-service/)
dotnet build                      # Compila proyecto
dotnet run                        # Ejecuta localmente
dotnet ef migrations add Name     # Crea migración
dotnet ef database update         # Aplica migraciones
dotnet add package PackageName    # Instala paquete NuGet

# Comandos Python (desde services/audit-service/)
pip install -r requirements.txt   # Instala dependencias
uvicorn main:app --reload         # Ejecuta con auto-reload
pip freeze > requirements.txt     # Actualiza lista de dependencias

# Acceso a Base de Datos (desde cualquier carpeta)
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db
```

## Pruebas

### Smoke Tests Manuales

Todas las pruebas documentadas en [docs/runbook.md](docs/runbook.md#smoke-tests--verification)

### Script de Prueba Rápida

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

echo "\nMVP Test Complete"
```

## Solución de Problemas

### Puerto ya está en uso

```bash
# Encuentra proceso usando puerto 5115
lsof -i :5115
kill -9 <PID>

# O usa un puerto diferente
# Edita docker-compose.yml: "5116:5115"
```

### Conexión a base de datos fallida

```bash
# Reset PostgreSQL
docker-compose down -v postgres
docker-compose up -d postgres
sleep 10
docker-compose up -d claims-service
```

### Migraciones no aplicadas

```bash
# Verifica logs
docker-compose logs claims-service | grep migration

# Aplica manualmente
cd services/claims-service
dotnet ef database update
```

Ver [docs/runbook.md](docs/runbook.md#troubleshooting) para más detalles.

## Decisiones Tecnológicas

### ¿Por qué .NET para Claims Service?

- Seguridad de tipos y validación en tiempo de compilación
- Madurez empresarial en industrias reguladas
- Ecosistema rico (EF Core, autenticación, logging)
- Alto performance para operaciones CRUD

### ¿Por qué Python/FastAPI para Audit Service?

- Desarrollo rápido (< 1 hora para construir MVP)
- Boilerplate mínimo para operaciones CRUD simples
- Documentación OpenAPI auto-generada
- Demuestra arquitectura de microservicios políglotos

### ¿Por qué PostgreSQL?

- Cumplimiento ACID para transacciones financieras
- Código abierto y libre (sin costos de licencia)
- Soporte JSON para datos híbrido relacional/documento
- Escalabilidad comprobada

Ver [docs/runbook.md](docs/runbook.md#technology-stack-overview) para razonamiento completo.

## Hoja de Ruta

### Fase 1: MVP (Completada)

- [x] API CRUD de Reclamaciones con persistencia PostgreSQL
- [x] Servicio de auditoría con almacenamiento en memoria
- [x] Orquestación Docker Compose
- [x] Integración HTTP entre servicios
- [x] Documentación completa

### Fase 2: Mejoras (Futuro)

- [ ] Persistencia de base de datos para audit-service
- [ ] Pruebas unitarias (xUnit + pytest)
- [ ] Middleware para manejo de errores
- [ ] Logging estructurado (Serilog)
- [ ] Autenticación JWT

### Fase 3: Frontend (Futuro)

- [ ] Dashboard Next.js
- [ ] UI para crear/listar/ver reclamaciones
- [ ] Cliente API tipado

### Fase 4: Producción (Futuro)

- [ ] Pipeline CI/CD (GitHub Actions)
- [ ] Manifiestos Kubernetes
- [ ] Monitoring (Prometheus + Grafana)
- [ ] Distributed Tracing (OpenTelemetry)

## Contribuir

1. Crea rama de feature: `git checkout -b feature/my-feature`
2. Realiza cambios y prueba localmente
3. Haz commit usando Conventional Commits: `git commit -m "feat: add new feature"`
4. Push y crea Pull Request

## Licencia

MIT License - Ver archivo LICENSE para detalles

## Soporte

- **Documentación:** [docs/runbook.md](docs/runbook.md)
- **Issues:** <https://github.com/SvillarroelZ/claimsops-app/issues>
- **Contacto:** Engineering Team

---

Construido como un MVP completo para gestión empresarial de reclamaciones de seguros.
