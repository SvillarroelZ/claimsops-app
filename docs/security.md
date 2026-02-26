# Security Guidelines

## Environment Variables and Secrets Management

### Local Development

**Critical Files:**
- `.env` - Contains actual secrets. **NEVER commit this file.**
- `.env.example` - Template with placeholder values. Safe to commit.

### Password Generation

For local development, generate secure passwords using:
```bash
openssl rand -base64 24
```

### What Goes Where

| Type | Example | Location | Commit? |
|------|---------|----------|---------|
| Actual secrets | `POSTGRES_PASSWORD=b+i8CEV+5YQ/...` | `.env` | NO |
| Templates | `POSTGRES_PASSWORD=your_generated_secure_password_here` | `.env.example` | YES |
| Configuration | `ServiceName`, `Version` | `appsettings.json` | YES |
| Dev overrides | Log levels | `appsettings.Development.json` | YES |

### Production Secrets Management

For production environments, use proper secrets management:
- **AWS**: AWS Secrets Manager or Parameter Store
- **Azure**: Azure Key Vault
- **GCP**: Google Secret Manager
- **Docker**: Docker Secrets
- **Kubernetes**: Kubernetes Secrets

**Never** hardcode production credentials in any file.

## API Security

### CORS Configuration

The API uses configurable CORS origins from `appsettings.json`:
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

For production, update this with your actual frontend domain(s).

### Authentication & Authorization

Currently not implemented (Phase 1-3). Will be added in later phases:
- JWT tokens for authentication
- Role-based access control (RBAC)
- API key validation for service-to-service communication

## Input Validation

All API endpoints validate input using:
- Data annotations on DTOs
- FluentValidation (to be added in Phase 4)
- Model state validation in controllers

## Error Handling

Production error responses do **not** expose:
- Stack traces
- Internal paths
- Database schema details
- Sensitive configuration

Use structured logging for debugging, not error responses.

## Database Security

### Connection Strings

Connection strings are built from environment variables, never hardcoded.

### Migrations

Database migrations are tracked in version control but do not contain:
- Seed data with real user information
- Production credentials
- Sensitive business data

## Logging

Logs must **never** contain:
- Passwords or secrets
- Full credit card numbers
- Social Security Numbers
- API keys or tokens
- Personal Identifiable Information (PII)

Use structured logging with log levels:
- `Error`: Exceptions and critical failures
- `Warning`: Recoverable issues
- `Information`: Normal operations
- `Debug`: Detailed flow (dev only)

## Dependencies

Regular security audits:
```bash
# .NET
dotnet list package --vulnerable

# Python
pip-audit

# Node.js
npm audit
```

## Checklist Before Commit

- [ ] No `.env` files in staged changes
- [ ] No hardcoded passwords or API keys
- [ ] `.env.example` has placeholder values only
- [ ] Configuration uses environment variables
- [ ] No sensitive data in logs
- [ ] CORS origins are explicitly defined
- [ ] Error messages don't leak internal details
