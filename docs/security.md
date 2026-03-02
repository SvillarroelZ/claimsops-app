# Security Guidelines

## Environment Variables and Secrets Management

### Local Development

**Critical Files:**

| File | Location | Purpose | Commit? |
|------|----------|---------|---------|
| `.env` | `docker/` folder | Contains actual secrets | **NO** ❌ |
| `.env.example` | `docker/` folder | Template with safe placeholders | **YES** ✅ |
| `.gitignore` | Repository root | Lists files to exclude from git | **YES** ✅ |

**Important:** The `.env` file must be located in the `docker/` folder (not in repository root).

```bash
# Correct ✅
docker/
  ├── .env           (NEVER commit)
  ├── .env.example   (commit this)
  └── docker-compose.yml

# Wrong ❌
.env               (at root - please don't do this)
```

**Verification that .env is ignored:**
```bash
# Check that .env is in .gitignore
grep "\.env" .gitignore

# Verify .env is not staged
git status | grep "\.env"  # Should show nothing
```

### Password Generation

For local development, generate secure passwords using:
```bash
openssl rand -base64 24
# Output: b+i8CEV+5YQ/1fdo4BwoSBKxY6Z8m7pl
```

Then copy to `docker/.env` (not to version control).

### What Goes Where

| Type | Example | Location | Commit? | Why? |
|------|---------|----------|---------|------|
| Actual secrets | `POSTGRES_PASSWORD=b+i8CEV+...` | `docker/.env` | ❌ NO | Security risk |
| Placeholders | `POSTGRES_PASSWORD=generate_secure_value_here` | `docker/.env.example` | ✅ YES | Template for setup |
| Configuration | Port numbers, service names | `appsettings.json` | ✅ YES | Non-sensitive |
| Dev overrides | Log levels, debug flags | `appsettings.Development.json` | ✅ YES | Dev-only settings |
| Paths & routes | API endpoints, folders | Code and docs | ✅ YES | Non-sensitive |

### Production Secrets Management

For production environments, use proper secrets management systems:

| Platform | Service | Example |
|----------|---------|---------|
| **AWS** | AWS Secrets Manager | `arn:aws:secretsmanager:region:account:secret:name` |
| **Azure** | Azure Key Vault | `https://vault.azure.net/secrets/name` |
| **GCP** | Google Secret Manager | `projects/{id}/secrets/{name}` |
| **Docker** | Docker Secrets | Swarm mode only |
| **Kubernetes** | Kubernetes Secrets | ConfigMap + Secret |
| **GitHub** | GitHub Secrets | `${{ secrets.DB_PASSWORD }}` in Actions |

**Rule:** Never commit or hardcode production credentials in any file. Use environment-specific secrets management.

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

## Checklist Before Commit

Before pushing code, verify:

```bash
# 1. No .env files staged
git status | grep "\.env$"

# 2. No hardcoded secrets in code
grep -r "password\|secret\|key\|token" services/ --exclude-dir=bin --exclude-dir=obj --exclude-dir=__pycache__

# 3. .env.example has only placeholders
grep "=\$\|=your_\|=replace_\|=generate_" docker/.env.example

# 4. Check .gitignore includes .env
cat .gitignore | grep "\.env"
```

**Checklist:**

- [ ] No `.env` files in staged changes (`git status` is clean)
- [ ] No hardcoded passwords, API keys, or secrets in source code
- [ ] `.env.example` has placeholder values only (no real secrets)
- [ ] Configuration uses environment variables (not hardcoded)
- [ ] No sensitive data in logs (passwords, SSN, credit cards, PII)
- [ ] CORS origins are explicitly configured
- [ ] Error messages don't leak internal system details
- [ ] `.gitignore` includes `.env`, `.env.local`, etc.
- [ ] All dependencies are up to date and pass security audits

## Verification Commands

### Check for committed secrets in history

```bash
# Search git history for common secret patterns
git log -S "password" --all --source --pretty=oneline
git log -S "secret" --all --source --pretty=oneline
git log -S "key" --all --source --pretty=oneline
```

### Verify .env is in .gitignore

```bash
cat .gitignore | grep -E "^\\.env"
```

Expected output:
```
.env
.env.local
.env.*.local
```

### Run dependency audits

```bash
# .NET packages
cd services/claims-service
dotnet list package --vulnerable

# Python packages
cd ../audit-service
pip-audit

# Check for known CVEs
# Use: https://nvd.nist.gov/ or https://www.cvedetails.com/
```
