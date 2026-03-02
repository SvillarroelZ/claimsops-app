# Docker Environment

Container configuration for ClaimsOps local development.

## Services

| Service | Image | Port | Description |
|---------|-------|------|-------------|
| **postgres** | postgres:15-alpine | 5432 | PostgreSQL database for claims data |
| **claims-service** | Built from Dockerfile | 5115 | C# .NET Web API for claims management |
| **audit-service** | Built from Dockerfile | 8000 | Python FastAPI microservice for audit events |

## Prerequisites

- Docker Engine 24.0+
- Docker Compose 2.20+

## Configuration

Environment variables are stored in `.env` file (not tracked in git).

### Setup

```bash
# Copy environment template
cp .env.example .env

# Generate a secure password for POSTGRES_PASSWORD
openssl rand -base64 24

# Edit .env and set POSTGRES_PASSWORD with the generated value
```

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `POSTGRES_USER` | Database username | claimsops_user | Yes |
| `POSTGRES_PASSWORD` | Database password | (none) | ✅ **Must be set** |
| `POSTGRES_DB` | Database name | claimsops_db | Yes |
| `POSTGRES_PORT` | Host port for PostgreSQL | 5432 | Yes |

## Commands

### Start All Services

```bash
# Modern syntax (recommended - run from project root)
docker compose -f docker/docker-compose.yml up -d

# OR from docker/ folder
cd docker
docker compose -f docker-compose.yml up -d

# OR old syntax (still works if you're in docker/ folder)
cd docker
docker-compose up -d

# Start with rebuild (after code changes)
docker compose -f docker/docker-compose.yml up -d --build
```

### Check Status

```bash
# Modern syntax
docker compose -f docker/docker-compose.yml ps

# OR from docker/ folder
cd docker
docker compose ps
```

### View Logs

```bash
# All services (modern syntax)
docker compose -f docker/docker-compose.yml logs

# PostgreSQL only
docker compose -f docker/docker-compose.yml logs postgres

# Claims service logs
docker compose -f docker/docker-compose.yml logs claims-service

# Follow logs in real-time
docker compose -f docker/docker-compose.yml logs -f

# Follow specific service
docker compose -f docker/docker-compose.yml logs -f claims-service
```

### Stop Services

```bash
# Stop containers (preserves data)
docker compose -f docker/docker-compose.yml down

# Stop and remove volumes (deletes all data)
docker compose -f docker/docker-compose.yml down -v

# Just stop without removing containers
docker compose -f docker/docker-compose.yml stop
```

### Connect to Database

```bash
# Using psql inside container
docker exec -it claimsops-postgres psql -U claimsops_user -d claimsops_db

# Using external client
psql -h localhost -p 5432 -U claimsops_user -d claimsops_db
```

## Connection Details

Applications connect to PostgreSQL using these settings:

| Property | Value (from host) | Value (from container) |
|----------|------------------|------------------------|
| Host | `localhost` | `postgres` |
| Port | `5432` | `5432` |
| Database | `claimsops_db` | `claimsops_db` |
| Username | `claimsops_user` | `claimsops_user` |
| Password | (from `.env` file) | (from `.env` file) |

### Connection String Format

**For .NET applications (claims-service):**
```
Host=postgres;Port=5432;Database=claimsops_db;Username=claimsops_user;Password=YOUR_PASSWORD
```

**For local development (connecting from host machine):**
```
Host=localhost;Port=5432;Database=claimsops_db;Username=claimsops_user;Password=YOUR_PASSWORD
```

## Troubleshooting

### Container won't start

```bash
# Check logs for errors
docker compose -f docker/docker-compose.yml logs postgres

# Verify .env file exists and has POSTGRES_PASSWORD set
cat docker/.env
```

### Port already in use

```bash
# Find process using port 5432
lsof -i :5432

# Use different port in .env
POSTGRES_PORT=5433
```

### Reset database

```bash
# Remove container and volume
docker compose -f docker/docker-compose.yml down -v

# Start fresh
docker compose -f docker/docker-compose.yml up -d
```
