# Docker Configuration Guide

This document provides a comprehensive comparison of Docker configurations for local development and production environments, ensuring consistency and clarity.

## Overview

TaskFlow.Api supports multiple Docker configurations optimized for different deployment scenarios:

- **Development**: Fast iteration with automatic migrations and development settings
- **Production**: Optimized build with production settings and manual migration control

## Configuration Comparison

### Dockerfiles

| Aspect | Development (`Dockerfile.dev`) | Production (`Dockerfile`) |
|--------|-------------------------------|--------------------------|
| **Build Context** | `./TaskFlow.Api` directory | Repository root (`.`) |
| **Base Images** | .NET 9 SDK → .NET 9 ASP.NET runtime | .NET 9 SDK → .NET 9 ASP.NET runtime |
| **Environment** | `ASPNETCORE_ENVIRONMENT=Development` | `ASPNETCORE_ENVIRONMENT=Production` |
| **Copy Strategy** | Single-stage from local dir | Multi-stage with explicit paths |
| **Database Path** | `/app/data/tasks.dev.db` (via appsettings) | `/app/data/tasks.db` (via appsettings) |
| **Auto Migrations** | Enabled by default | Disabled by default |
| **Port** | 8080 | 8080 |
| **Swagger UI** | Enabled | Disabled |
| **Optimizations** | Standard | Release build with `--no-restore` |

### Docker Compose Files

| Aspect | Development (`docker-compose.yml`) | Production (`docker-compose.prod.yml`) |
|--------|-----------------------------------|--------------------------------------|
| **Build Context** | `./TaskFlow.Api` | `.` (repository root) |
| **Dockerfile** | `Dockerfile.dev` | `TaskFlow.Api/Dockerfile` |
| **Container Name** | `taskflow-api` | `taskflow-api-prod` |
| **Image Tag** | `taskflow-api:dev` | `taskflow-api:prod` |
| **Environment** | `Development` | `Production` |
| **Auto Migrations** | `Database__MigrateOnStartup=true` | `Database__MigrateOnStartup=false` |
| **Volumes** | `./data`, `./logs` | `./data`, `./logs` |
| **Health Check** | Enabled (40s start period) | Enabled (40s start period) |
| **Port Mapping** | `8080:8080` | `8080:8080` |

### Environment Variables

#### Common to Both Environments

| Variable | Value | Purpose |
|----------|-------|---------|
| `ASPNETCORE_URLS` | `http://+:8080` | Configure Kestrel to listen on all interfaces |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Explicitly set HTTP port |
| `DOTNET_RUNNING_IN_CONTAINER` | `true` | Signal container runtime |

#### Development-Specific

| Variable | Value | Purpose |
|----------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Enable dev features (Swagger, detailed errors) |
| `Database__MigrateOnStartup` | `true` | Auto-apply migrations on startup |

#### Production-Specific

| Variable | Value | Purpose |
|----------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Enable production optimizations |
| `Database__MigrateOnStartup` | `false` | Prevent automatic migrations (manual control) |

### Volume Mounts

**Both environments use identical volume mounts:**

| Host Path | Container Path | Purpose |
|-----------|---------------|---------|
| `./data` | `/app/data` | Persist SQLite database |
| `./logs` | `/app/logs` | Persist application logs |

**Important Notes:**
- These directories are created automatically on first run
- Both are excluded from version control via `.gitignore`
- Database files differ by environment: `tasks.dev.db` vs `tasks.db`
- For detailed volume configuration, see [volumes.md](volumes.md)

### Health Checks

**Both environments use identical health check configuration:**

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s        # Check every 30 seconds
  timeout: 10s         # Allow 10 seconds for response
  retries: 3           # Mark unhealthy after 3 failures
  start_period: 40s    # Grace period for startup and migrations
```

**Why identical?**
- Both environments may run migrations on startup
- Consistent health monitoring across environments
- Prevents restart loops during database initialization

## Required Differences

Some differences between environments are intentional and necessary:

### 1. Build Context Difference

**Reason**: Development optimizes for local iteration, production for complete builds.

- **Development**: Build context is `./TaskFlow.Api` to minimize Docker build context size during rapid iteration
- **Production**: Build context is repository root (`.`) to support multi-project solutions and CI/CD pipelines

**Impact**: When building manually, you must be in the correct directory or use docker-compose.

### 2. Environment Variable Difference

**Reason**: Different features and behaviors needed per environment.

- **Development**: Enables Swagger UI, detailed error pages, and verbose logging
- **Production**: Disables Swagger, shows generic error pages, optimized logging

**Impact**: Production-like testing requires explicitly setting `ASPNETCORE_ENVIRONMENT=Production`.

### 3. Migration Strategy Difference

**Reason**: Production deployments require controlled migration execution.

- **Development**: Automatic migrations (`Database__MigrateOnStartup=true`) for convenience
- **Production**: Manual migrations (`Database__MigrateOnStartup=false`) to prevent concurrent migration issues in scaled deployments

**Impact**: Production deployments must explicitly run migrations or set the flag.

## Usage Scenarios

### Local Development with Hot Reload

**Use Case**: Active development with frequent code changes.

**Method**: Docker Compose Development

```bash
# From repository root
docker-compose up
```

**Characteristics:**
- ✅ Fast startup
- ✅ Development environment
- ✅ Automatic migrations
- ✅ Swagger UI enabled
- ✅ Verbose logging

### Production-Like Local Testing

**Use Case**: Test production configuration before deployment.

**Method**: Docker Compose Production

```bash
# From repository root
docker-compose -f docker-compose.prod.yml up
```

**Before first run, apply migrations manually:**
```bash
# Option 1: Use dotnet CLI (if .NET 9 SDK installed locally)
dotnet ef database update --project TaskFlow.Api

# Option 2: Run container with migrations enabled for first run
docker compose -f docker-compose.prod.yml run --rm -e Database__MigrateOnStartup=true taskflow-api
```

**Characteristics:**
- ✅ Production environment
- ✅ Manual migration control
- ✅ Swagger UI disabled
- ✅ Production logging
- ⚠️ Requires explicit migration

### CI/CD Production Build

**Use Case**: Automated deployment pipeline.

**Method**: Direct Docker build from repo root

```bash
# Build production image
docker build -f TaskFlow.Api/Dockerfile -t taskflow-api:latest .

# Run with production settings
docker run -d -p 8080:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Database__MigrateOnStartup=false \
  --name taskflow-api taskflow-api:latest
```

**Characteristics:**
- ✅ Optimized build
- ✅ No compose dependency
- ✅ Suitable for orchestrators (K8s, ECS)
- ✅ Explicit configuration via env vars

### Kubernetes Production Deployment

**Use Case**: Cloud-native container orchestration.

**Method**: Use `k8s/deployment.yaml`

```bash
kubectl apply -f k8s/deployment.yaml
```

**Characteristics:**
- ✅ Production environment
- ✅ Health probes configured
- ✅ Persistent volumes
- ✅ Resource limits
- ✅ Rolling updates

## Build Context Explained

Understanding build context is crucial for successful Docker builds:

### Development Build Context

```
docker build -f Dockerfile.dev .
# Build context: ./TaskFlow.Api directory
# Dockerfile location: ./TaskFlow.Api/Dockerfile.dev
```

**Directory structure during build:**
```
/src (in container)
└── TaskFlow.Api.csproj    # Copied from .
└── *.cs files             # Copied from .
```

### Production Build Context

```
docker build -f TaskFlow.Api/Dockerfile .
# Build context: . (repository root)
# Dockerfile location: ./TaskFlow.Api/Dockerfile
```

**Directory structure during build:**
```
/src (in container)
└── TaskFlow.Api/
    └── TaskFlow.Api.csproj    # Copied from TaskFlow.Api/
    └── *.cs files             # Copied from TaskFlow.Api/
```

**Why this matters:**
- Production context supports future multi-project solutions
- Development context optimizes for single-project iteration
- Both approaches are valid for their use cases

## Testing Configurations Locally

### Verify Development Configuration

```bash
# Build and run development
docker-compose up -d

# Check environment
docker exec taskflow-api printenv | grep ASPNETCORE_ENVIRONMENT
# Expected: ASPNETCORE_ENVIRONMENT=Development

# Check database file
docker exec taskflow-api ls -la /app/data/
# Expected: tasks.dev.db exists

# Check Swagger
curl http://localhost:8080/swagger
# Expected: HTTP 200 with Swagger UI

# Check health
curl http://localhost:8080/health
# Expected: {"status":"Healthy",...}

# Cleanup
docker-compose down
```

### Verify Production Configuration

```bash
# Build and run production
docker-compose -f docker-compose.prod.yml up -d

# Check environment
docker exec taskflow-api-prod printenv | grep ASPNETCORE_ENVIRONMENT
# Expected: ASPNETCORE_ENVIRONMENT=Production

# Check database file
docker exec taskflow-api-prod ls -la /app/data/
# Expected: tasks.db exists (or none if migrations not run)

# Check Swagger (should be disabled)
curl http://localhost:8080/swagger
# Expected: HTTP 404 or redirect to /

# Check health
curl http://localhost:8080/health
# Expected: {"status":"Healthy",...}

# Cleanup
docker-compose -f docker-compose.prod.yml down
```

## Common Issues and Solutions

### Issue: "Could not find file '/src/TaskFlow.Api.csproj'"

**Cause**: Incorrect build context.

**Solution**: 
- For development: `cd TaskFlow.Api && docker build -f Dockerfile.dev .`
- For production: `docker build -f TaskFlow.Api/Dockerfile .` (from repo root)
- Or use docker-compose: `docker-compose up` or `docker-compose -f docker-compose.prod.yml up`

### Issue: "Database migrations not applied in production"

**Cause**: `Database__MigrateOnStartup=false` in production.

**Solution**:
```bash
# Option 1: Run container with migrations enabled
docker compose -f docker-compose.prod.yml run --rm \
  -e Database__MigrateOnStartup=true taskflow-api

# Option 2: Run migrations via dotnet CLI
dotnet ef database update --project TaskFlow.Api
```

### Issue: "Swagger UI not working in production"

**Cause**: Swagger is disabled in production environment (by design).

**Solution**: This is expected behavior. To test with Swagger:
```bash
# Use development compose
docker-compose up

# Or override environment
docker run -e ASPNETCORE_ENVIRONMENT=Development ...
```

### Issue: "Container restarts immediately"

**Cause**: Health check failing during startup.

**Solution**: Check the start_period (40s grace period):
```bash
# View logs
docker logs taskflow-api

# Check if migrations are hanging
docker exec taskflow-api ls -la /app/data/

# Verify health check endpoint manually
docker exec taskflow-api curl http://localhost:8080/health
```

## Best Practices

### 1. Use Docker Compose for Local Development

**Why**: Simplifies commands and ensures correct build context.

```bash
# Good
docker-compose up

# Less convenient
cd TaskFlow.Api && docker build -f Dockerfile.dev -t taskflow-api:dev . && \
  docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development ...
```

### 2. Test Production Configuration Locally

**Why**: Catch configuration issues before deployment.

```bash
# Test production config before pushing
docker-compose -f docker-compose.prod.yml up
```

### 3. Use Explicit Environment Variables

**Why**: Prevents ambiguity and unexpected behavior.

```bash
# Good
docker run -e ASPNETCORE_ENVIRONMENT=Production ...

# Risky (relies on Dockerfile defaults)
docker run ...
```

### 4. Separate Data by Environment

**Why**: Prevents mixing development and production-like test data.

- Development: `/app/data/tasks.dev.db`
- Production: `/app/data/tasks.db`

Both can coexist in the same `./data` volume mount.

### 5. Always Use Volume Mounts for Persistence

**Why**: Data persists across container restarts.

```bash
# Good
docker run -v $(pwd)/data:/app/data ...

# Bad (data lost on container removal)
docker run ...
```

## Summary

### Intentional Differences (Keep These)

| Aspect | Reason |
|--------|--------|
| Build context (local dir vs repo root) | Optimizes for different workflows |
| `ASPNETCORE_ENVIRONMENT` setting | Enables appropriate features per environment |
| `Database__MigrateOnStartup` flag | Production requires migration control |
| Database filename | Separates dev and prod data |

### Aligned Settings (Already Consistent)

| Aspect | Value |
|--------|-------|
| Port | 8080 |
| Volume mounts | `./data:/app/data`, `./logs:/app/logs` |
| Health check configuration | 30s interval, 40s start period |
| Base images | .NET 9 SDK and ASP.NET runtime |
| Directory permissions | `chmod 777 /app/logs /app/data` |

### New Additions for Consistency

- ✅ `docker-compose.prod.yml` for production-like local testing
- ✅ This comprehensive configuration documentation
- ✅ Side-by-side comparison tables
- ✅ Testing procedures for both configurations

## Related Documentation

- [Main README](../README.md) - Getting started and deployment
- [Volume Configuration](volumes.md) - Detailed volume setup
- [Kubernetes Deployment](../k8s/README.md) - K8s-specific configuration
- [Security Scanning](SECURITY_SCANNING.md) - Docker image security

## Feedback and Improvements

If you encounter configuration inconsistencies not covered in this document, please:
1. Check that you're using the correct build context
2. Verify environment variables match your intended environment
3. Review the troubleshooting section
4. Open an issue with details about the discrepancy
