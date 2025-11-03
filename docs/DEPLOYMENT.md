# Deployment Guide

Comprehensive guide for deploying TaskFlow.Api locally with Docker and to Azure cloud environments.

## Table of Contents

- [Docker Deployment](#docker-deployment)
- [Azure Deployment](#azure-deployment)
- [CI/CD Workflows](#cicd-workflows)
- [Environment Configuration](#environment-configuration)
- [Troubleshooting](#troubleshooting)

## Docker Deployment

### Quick Start

**Development (with auto-migrations):**
```bash
docker compose up
```

**Production-like (manual migrations):**
```bash
docker compose -f docker-compose.prod.yml up
```

### Docker Configuration Overview

TaskFlow.Api provides two Docker configurations for different deployment scenarios.

> **📖 For detailed Docker configuration, see:**
> - [DOCKER_CONFIGURATION.md](DOCKER_CONFIGURATION.md) - Comprehensive dev vs prod comparison
> - [VOLUMES.md](VOLUMES.md) - Volume configuration and persistence
> - [HEALTH_CHECK_TESTING.md](HEALTH_CHECK_TESTING.md) - Health check setup and testing

**Quick comparison:**

| Configuration | Use Case | Auto-migrations | Swagger |
|--------------|----------|----------------|---------|
| **Development** (`docker-compose.yml`) | Local dev, fast iteration | ✅ Enabled | ✅ Enabled |
| **Production** (`docker-compose.prod.yml`) | Production builds, Azure | ❌ Manual | ❌ Disabled |

### Development Deployment

**Quick start:**
```bash
docker compose up
```

Access at `http://localhost:8080` (Swagger UI enabled)

**Common commands:**
```bash
# View logs
docker compose logs -f

# Stop (preserves data)
docker compose down

# Stop and remove data
docker compose down -v
```

> **📖 For detailed instructions, see [DOCKER_CONFIGURATION.md](DOCKER_CONFIGURATION.md)**

### Production Deployment

**Quick start:**
```bash
# Apply migrations first (one-time)
dotnet ef database update --project TaskFlow.Api

# Start production containers
docker compose -f docker-compose.prod.yml up -d

# Verify
curl http://localhost:8080/health
```

> **📖 For detailed instructions including Docker CLI usage, see [DOCKER_CONFIGURATION.md](DOCKER_CONFIGURATION.md)**

## Azure Deployment

TaskFlow.Api supports automated Azure deployment using GitHub Actions workflows.

### Prerequisites

1. **Azure subscription** with appropriate permissions
2. **Service principal** with Contributor access
3. **GitHub repository secrets** configured for OIDC authentication:
   - `AZURE_CLIENT_ID` - Application (client) ID
   - `AZURE_TENANT_ID` - Directory (tenant) ID  
   - `AZURE_SUBSCRIPTION_ID` - Azure subscription ID
4. **GitHub environments** configured: `qa` and `production`

### Setting Up Azure OIDC Authentication

TaskFlow.Api uses OpenID Connect (OIDC) for secure, passwordless authentication to Azure.

**Quick setup:**
1. Create Azure service principal with Contributor role
2. Configure federated credentials for GitHub Actions
3. Add secrets to GitHub: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
4. Create GitHub environments: `qa` and `production`

> **📖 For detailed OIDC setup with complete commands, see [AZURE_OIDC_AUTHENTICATION.md](AZURE_OIDC_AUTHENTICATION.md)**

### Production Deployment to Azure Container Instances

The production deployment workflow deploys to Azure Container Instances (ACI), providing a cost-effective solution without App Service Plan quota limitations.

**Trigger deployment:**

Option 1: Tag-based (recommended)
```bash
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

Option 2: Manual trigger
1. Go to **Actions** tab in GitHub
2. Select **Deploy to Azure Production** workflow
3. Click **Run workflow**

**What gets deployed:**
- **Resource Group:** `nevridge-taskflow-prod-rg`
- **Azure Container Registry:** `nevridgetaskflowprodacr`
- **ACI Container:** `nevridge-taskflow-prod-aci`
- **DNS Label:** `taskflow-prod`
- **Public URL:** `http://taskflow-prod.eastus.azurecontainer.io:8080`

**Workflow steps:**
1. Builds Docker image via Azure Container Registry
2. Creates Azure resources (resource group and ACR if they don't exist)
3. Deletes existing ACI container (if present)
4. Creates new ACI container with latest image
5. Verifies deployment via health check

**Benefits:**
- **No App Service Plan quota requirements**
- **Cost-effective for deploy/test/teardown workflows**
- **Fast deployment and teardown cycles**
- **Consistent with QA environment approach**

### QA Deployment to Azure Container Instances

The QA workflow creates ephemeral test environments with **fixed, predictable DNS names** for consistent testing.

**Quick start:**
1. Go to **Actions** → **Ephemeral ACI deploy - create test teardown**
2. Click **Run workflow** with `action: deploy`
3. Access QA endpoint: `http://taskflow-qa.{region}.azurecontainer.io:8080`

**Use cases:**
- Postman collections with static QA URLs
- Automated test suites with consistent endpoints
- Demo environments

> **📖 For detailed QA deployment instructions, Postman setup, and troubleshooting, see [QA_DEPLOYMENT.md](QA_DEPLOYMENT.md)**

### Production Teardown

A separate workflow safely tears down production resources.

⚠️ **WARNING:** This permanently deletes all production resources and data.

**To teardown:**
1. Go to **Actions** tab
2. Select **Production Teardown** workflow
3. Click **Run workflow**
4. Type **"CONFIRM"** exactly in the confirmation field
5. Click **Run workflow**

**What gets deleted:**
- Entire production resource group
- ACI Container
- Container Registry
- All data, logs, and configurations

**Safety features:**
- Manual trigger only
- Confirmation required
- Separate workflow file
- Clear logging of resources to be deleted

### Azure Naming Convention

All Azure resources follow a standardized naming pattern:

```
{org}-{app}-{env}-{resourceType}
```

**Example:**
- Resource Group: `nevridge-taskflow-prod-rg`
- Web App: `nevridge-taskflow-prod-web`
- App Service Plan: `nevridge-taskflow-prod-plan`
- ACR: `nevridgetaskflowprodacr` (no hyphens for ACR)

To customize, edit the workflow files and update these variables:
- `ORG_NAME` - Organization identifier (e.g., `nevridge`)
- `APP_NAME` - Application name (e.g., `taskflow`)
- `ENV` - Environment (e.g., `prod`, `qa`)

For complete naming convention details, see [DEPLOY.md](DEPLOY.md).

## CI/CD Workflows

### Build and Test Workflow

**File:** `.github/workflows/ci.yml`

**Triggers:**
- Push to any branch
- Pull requests

**Jobs:**
1. **Lint:** Validates C# code formatting with `dotnet format`
2. **Build:** Compiles solution in Release configuration
3. **Test:** Runs tests with code coverage enforcement (58% minimum)

**Code Coverage:**
- Enforces minimum 58% line coverage
- Generates detailed coverage reports
- Build fails if coverage drops below threshold

### Security Scanning Workflows

**CodeQL (SAST):**
- File: `.github/workflows/codeql.yml`
- Scans C# code for security vulnerabilities
- Triggers: Push to main, PRs, weekly schedule
- Results: GitHub Security tab

**Trivy (Container Scanning):**
- File: `.github/workflows/security-scan.yml`
- Scans Docker images for vulnerabilities
- Fails on CRITICAL/HIGH severity findings
- Triggers: Push to main, PRs, weekly schedule

For detailed security scanning documentation, see [SECURITY_SCANNING.md](SECURITY_SCANNING.md).

### Deployment Workflows

**Production Deploy:**
- File: `.github/workflows/prod-deploy.yaml`
- Triggers: Tags matching `v*`, manual
- Environment: `production`
- Deploys to Azure Container Instances (ACI)

**QA Deploy:**
- File: `.github/workflows/qa-deploy.yaml`
- Triggers: Manual only
- Environment: `qa`
- Deploys to Azure Container Instances (ACI)

**Production Teardown:**
- File: `.github/workflows/prod-teardown.yaml`
- Triggers: Manual only with confirmation
- Environment: `production`
- Deletes all production resources

## Environment Configuration

### Environment Variables

**Core Settings:**

| Variable | Default | Purpose |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Controls environment-specific behavior |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/tasks.db` | SQLite database path |
| `Database__MigrateOnStartup` | `false` (true in Development) | Enable automatic migrations |
| `LOG_PATH` | `/app/logs/log.txt` | Log file location |

**Azure Settings:**

| Variable | Purpose |
|----------|---------|
| `ApplicationInsights__ConnectionString` | Enable Application Insights telemetry |
| `ASPNETCORE_URLS` | Configure Kestrel listen URLs |
| `WEBSITE_PORT` | Azure App Service port configuration |

### Configuration Files

**appsettings.json** - Base configuration for all environments
**appsettings.Development.json** - Development overrides
**appsettings.Production.json** - Production overrides (optional)

**Override order (highest to lowest priority):**
1. Environment variables
2. `appsettings.{Environment}.json`
3. `appsettings.json`

### Docker Environment Configuration

**docker-compose.yml (Development):**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:8080
  - Database__MigrateOnStartup=true
```

**docker-compose.prod.yml (Production):**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:8080
  - Database__MigrateOnStartup=false
```

## Troubleshooting

### Docker Issues

**Container won't start:**
```bash
# Check logs
docker compose logs

# Check container status
docker ps -a

# Inspect specific container
docker logs taskflow-api
```

**Port already in use:**
```bash
# Find process using port 8080
lsof -i :8080  # macOS/Linux
netstat -ano | findstr :8080  # Windows

# Use different port in docker-compose.yml
ports:
  - "8081:8080"
```

**Database locked errors:**
- Ensure only one container accesses the database
- Stop other instances: `docker compose down`
- Check volume mounts are correct

**Health check failing:**
```bash
# Check health status
docker inspect --format='{{json .State.Health}}' taskflow-api

# Test health endpoint manually
curl http://localhost:8080/health

# View detailed container logs
docker logs taskflow-api --tail 100
```

### Azure Deployment Issues

**Authentication failures:**
- Verify OIDC secrets are correct: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- Ensure service principal has Contributor role
- Check federated credentials match GitHub environment names

**Health check failures post-deployment:**
```bash
# View ACI logs
az container logs --name {ACI_NAME} --resource-group {RESOURCE_GROUP}

# Get detailed container info
az container show --name {ACI_NAME} --resource-group {RESOURCE_GROUP}
```

**Container deployment issues:**
- Verify ACR credentials are accessible
- Confirm image exists: `az acr repository show -n {ACR_NAME} --image taskflowapi:latest`
- Check ACI container state: `az container show --name {ACI_NAME} --query instanceView.state`
- View container events: `az container show --name {ACI_NAME} --query instanceView.events`

### Migration Issues

**Migrations not applied:**
```bash
# Manually apply migrations
dotnet ef database update --project TaskFlow.Api

# Or enable auto-migration temporarily
docker run -e Database__MigrateOnStartup=true taskflow-api:latest
```

**Migration conflicts:**
```bash
# List migrations
dotnet ef migrations list --project TaskFlow.Api

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project TaskFlow.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project TaskFlow.Api
```

### Logging and Debugging

**Enable verbose logging:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    }
  }
}
```

**View application logs:**
```bash
# Docker
docker logs taskflow-api --follow

# Docker Compose
docker compose logs -f taskflow-api

# Azure App Service
az webapp log tail --name {WEBAPP_NAME} --resource-group {RESOURCE_GROUP}
```

**Check health check logs:**
Health check failures are automatically logged. Look for:
```
[ERR] Health check database failed: Unable to connect to database
[WRN] Health check database is Degraded
```

For comprehensive logging documentation, see [LOGGING.md](LOGGING.md).

## Additional Resources

- [Docker Configuration Details](DOCKER_CONFIGURATION.md) - Detailed Docker configuration comparison
- [Volume Configuration](VOLUMES.md) - Docker volume management
- [Azure OIDC Authentication](AZURE_OIDC_AUTHENTICATION.md) - Detailed OIDC setup
- [QA Deployment Guide](QA_DEPLOYMENT.md) - Ephemeral QA environment details
- [Resource Naming Convention](DEPLOY.md) - Azure naming standards
- [Security Scanning](SECURITY_SCANNING.md) - CodeQL and Trivy configuration

---

[← Back to README](../README.md) | [Architecture](ARCHITECTURE.md) | [Contributing →](CONTRIBUTING.md)
