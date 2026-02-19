# Docker Environment

Container configuration for ClaimsOps local development.

## Services

| Service | Image | Port | Description |
|---------|-------|------|-------------|
| postgres | postgres:15-alpine | 5432 | PostgreSQL database for claims and audit data |

## Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+

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

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_USER` | Database username | claimsops_user |
| `POSTGRES_PASSWORD` | Database password | (none - must be set) |
| `POSTGRES_DB` | Database name | claimsops_db |
| `POSTGRES_PORT` | Host port for PostgreSQL | 5432 |

## Commands

### Start Services

```bash
docker-compose up -d
```

### Check Status

```bash
docker-compose ps
```

### View Logs

```bash
# All services
docker-compose logs

# PostgreSQL only
docker-compose logs postgres

# Follow logs in real-time
docker-compose logs -f postgres
```

### Stop Services

```bash
# Stop containers (preserves data)
docker-compose down

# Stop and remove volumes (deletes all data)
docker-compose down -v
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

| Property | Value |
|----------|-------|
| Host | localhost (from host) or claimsops-postgres (from container) |
| Port | 5432 |
| Database | claimsops_db |
| Username | claimsops_user |
| Password | (from .env file) |

### Connection String Format

```
Host=localhost;Port=5432;Database=claimsops_db;Username=claimsops_user;Password=YOUR_PASSWORD
```

## Troubleshooting

### Container won't start

```bash
# Check logs for errors
docker-compose logs postgres

# Verify .env file exists and has POSTGRES_PASSWORD set
cat .env
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
docker-compose down -v

# Start fresh
docker-compose up -d
```
