# Docker Environment

This directory contains Docker Compose configuration for local development.

## Services

- **PostgreSQL 15**: Shared database for claims-service and audit-service

## Setup

1. Copy the environment template:
   ```bash
   cp .env.example .env
   ```

2. (Optional) Edit `.env` with your preferred credentials

3. Start the database:
   ```bash
   docker-compose up -d
   ```

4. Verify it's running:
   ```bash
   docker-compose ps
   ```

5. View logs:
   ```bash
   docker-compose logs postgres
   ```

## Connection Details

- **Host**: localhost
- **Port**: 5432 (default)
- **Database**: claimsops_db
- **User**: claimsops_user
- **Password**: (from .env file)

## Stopping Services

```bash
docker-compose down
```

To remove volumes (delete all data):
```bash
docker-compose down -v
```
