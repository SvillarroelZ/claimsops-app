# Audit Service

FastAPI microservice for recording audit events from the claims service.

## Overview

The Audit Service is a lightweight Python-based API that records all claim-related events for auditing and compliance purposes. It receives HTTP POST requests from the claims-service and stores events in memory.

## Architecture

```
Claims Service
    |
    | HTTP POST /audit
    v
[Audit Service]
    |
    v
[In-Memory Storage]  (Python list)
```

### Design Decisions

**Why Python + FastAPI?**
- Rapid development (MVP built in < 1 hour)
- Minimal boilerplate for simple CRUD operations
- Auto-generated OpenAPI documentation (Swagger UI)
- Async support for high concurrency
- Demonstrates polyglot microservices architecture

**Why In-Memory Storage?**
- Simple for MVP
- No database overhead
- Fast read/write operations
- **Trade-off:** Events are lost on container restart

**Future Enhancement:** Persist to PostgreSQL or dedicated audit database.

## API Endpoints

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| GET | `/health` | Service health check | 200 OK |
| POST | `/audit` | Record new audit event | 201 Created |
| GET | `/audit` | List all audit events | 200 OK |
| GET | `/audit?claim_id={id}` | Filter events by claim ID | 200 OK |
| GET | `/docs` | Interactive API documentation (Swagger UI) | HTML |

## Running Locally

### Prerequisites

- Python 3.11+
- pip (Python package manager)

### Setup

```bash
# Navigate to service directory
cd services/audit-service

# Create virtual environment
python3 -m venv venv

# Activate virtual environment
source venv/bin/activate  # Linux/Mac
venv\Scripts\activate     # Windows

# Install dependencies
pip install -r requirements.txt
```

### Run Service

```bash
# Development (with auto-reload)
uvicorn main:app --reload --port 8000

# Production
uvicorn main:app --host 0.0.0.0 --port 8000
```

Service will be available at:
- **API:** http://localhost:8000
- **Health Check:** http://localhost:8000/health
- **Swagger UI:** http://localhost:8000/docs
- **ReDoc:** http://localhost:8000/redoc

## API Examples

### Health Check

```bash
curl http://localhost:8000/health | jq .
```

**Response:**
```json
{
  "status": "healthy",
  "service": "audit-service",
  "timestamp": "2026-03-02T00:00:00.000000"
}
```

### Create Audit Event

```bash
curl -X POST http://localhost:8000/audit \
  -H "Content-Type: application/json" \
  -d '{
    "claim_id": "550e8400-e29b-41d4-a716-446655440000",
    "event_type": "created",
    "user_id": "system",
    "details": "Claim created for member MBR-12345"
  }' | jq .
```

**Response:**
```json
{
  "id": "7e3c2a9f-8b1d-4e6c-9a2b-5f1c8e9d7a3b",
  "status": "recorded"
}
```

### List All Events

```bash
curl http://localhost:8000/audit | jq .
```

**Response:**
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

### Filter by Claim ID

```bash
curl "http://localhost:8000/audit?claim_id=550e8400-e29b-41d4-a716-446655440000" | jq .
```

## Development

### Project Structure

```
audit-service/
├── main.py            # FastAPI application and all endpoints
├── requirements.txt   # Python dependencies
├── Dockerfile         # Container definition
└── README.md          # This file
```

### Dependencies

| Package | Purpose |
|---------|---------|
| `fastapi` | Web framework |
| `uvicorn[standard]` | ASGI server |
| `pydantic` | Data validation |

### Adding Dependencies

```bash
# Install new package
pip install package-name

# Update requirements.txt
pip freeze > requirements.txt
```

### Code Style

- Follow [PEP 8](https://peps.python.org/pep-0008/)
- Use type hints for function parameters and returns
- Add docstrings for functions
- Keep functions focused and small

### Testing

```bash
# Install pytest
pip install pytest pytest-asyncio httpx

# Run tests
pytest

# With coverage
pytest --cov=. --cov-report=html
```

## Docker

### Build Image

```bash
docker build -t audit-service:latest .
```

### Run Container

```bash
docker run -d \
  --name audit-service \
  -p 8000:8000 \
  audit-service:latest
```

### Docker Compose

The service is included in `docker/docker-compose.yml`:

```bash
cd docker
docker compose up -d audit-service
```

## Monitoring

### Health Checks

Docker health check configured in `docker-compose.yml`:

```yaml
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:8000/health || exit 1"]
  interval: 10s
  timeout: 5s
  retries: 5
```

### Logs

```bash
# Docker logs
docker logs -f claimsops-audit-service

# Docker Compose logs
docker compose -f docker/docker-compose.yml logs -f audit-service
```

## Known Limitations

1. **No Persistence:** Events stored in memory are lost on restart
2. **No Authentication:** Endpoints are publicly accessible
3. **No Rate Limiting:** Vulnerable to abuse
4. **Single Instance:** Not horizontally scalable (in-memory state)

## Future Enhancements

- [ ] Persist events to PostgreSQL
- [ ] Add authentication (API keys or JWT)
- [ ] Implement rate limiting
- [ ] Add event search and filtering
- [ ] Add pagination for large result sets
- [ ] Add metrics (Prometheus)
- [ ] Add distributed tracing (OpenTelemetry)

## Troubleshooting

### Port already in use

```bash
# Find process using port 8000
lsof -i :8000

# Kill process
kill -9 <PID>

# Or use different port
uvicorn main:app --port 8001
```

### Module not found

```bash
# Ensure virtual environment is activated
which python
# Should show path inside venv/

# Reinstall dependencies
pip install -r requirements.txt
```

### Connection refused from claims-service

Ensure audit-service is reachable:

```bash
# From host machine
curl http://localhost:8000/health

# From claims-service container
docker exec -it claimsops-claims-service curl http://audit-service:8000/health
```

## Links

- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Uvicorn Documentation](https://www.uvicorn.org/)
- [Pydantic Documentation](https://docs.pydantic.dev/)
- [Main Project README](../../README.md)
- [Technical Runbook](../../docs/runbook.md)
