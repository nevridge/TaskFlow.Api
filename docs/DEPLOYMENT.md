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

TaskFlow.Api provides two Docker configurations:

| Configuration | Dockerfile | Compose File | Use Case |
|--------------|------------|--------------|----------|
| **Development** | `Dockerfile.dev` | `docker-compose.yml` | Local development, fast iteration |
| **Production** | `Dockerfile` | `docker-compose.prod.yml` | Production builds, Azure deployment |

**Key Differences:**
- **Build context:** Dev uses `TaskFlow.Api/`, prod uses repository root
- **Auto-migrations:** Enabled in dev, disabled in prod
- **Swagger UI:** Enabled in dev, disabled in prod
- **Database file:** `tasks.dev.db` vs `tasks.db`

### Development Deployment

#### Using Docker Compose (Recommended)

1. **Start the application:**
   ```bash
   docker compose up
   ```

2. **Access the API:**
   - API: `http://localhost:8080`
   - Swagger UI: `http://localhost:8080`
   - Health check: `http://localhost:8080/health`

3. **View logs:**
   ```bash
   docker compose logs -f
   ```

4. **Stop and clean up:**
   ```bash
   # Stop and remove containers (preserves data)
   docker compose down
   
   # Remove data volumes as well
   docker compose down -v
   ```

**Features:**
- Automatic database migrations
- Persistent data via Docker volumes
- Hot reload support
- Development-optimized logging

#### Using Docker CLI

```bash
# Build image
cd TaskFlow.Api
docker build -f Dockerfile.dev -t taskflow-api:dev .

# Create volumes (optional - Docker creates automatically)
docker volume create taskflow-data
docker volume create taskflow-logs

# Run container
docker run -p 8080:8080 \
  -v taskflow-data:/app/data \
  -v taskflow-logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Database__MigrateOnStartup=true \
  taskflow-api:dev
```

### Production Deployment

#### Using Docker Compose

1. **Apply migrations first:**
   ```bash
   # Option 1: Using .NET CLI (if installed locally)
   dotnet ef database update --project TaskFlow.Api
   
   # Option 2: Run container with migrations enabled once
   docker compose -f docker-compose.prod.yml run --rm \
     -e Database__MigrateOnStartup=true taskflow-api
   ```

2. **Start production containers:**
   ```bash
   docker compose -f docker-compose.prod.yml up -d
   ```

3. **Verify deployment:**
   ```bash
   curl http://localhost:8080/health
   ```

#### Using Docker CLI

```bash
# Build production image from repository root
docker build -f TaskFlow.Api/Dockerfile -t taskflow-api:latest .

# Create production volumes
docker volume create taskflow-prod-data
docker volume create taskflow-prod-logs

# Run with production configuration
docker run -d -p 8080:8080 \
  -v taskflow-prod-data:/app/data \
  -v taskflow-prod-logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Database__MigrateOnStartup=false \
  --name taskflow-api taskflow-api:latest
```

### Volume Persistence

Both configurations use Docker volumes to persist data:

| Volume | Container Path | Purpose |
|--------|---------------|---------|
| `taskflow-data` | `/app/data` | SQLite database files |
| `taskflow-logs` | `/app/logs` | Application logs |

**Managing volumes:**
```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect taskflow-data

# Remove volume (deletes data!)
docker volume rm taskflow-data
```

**Important:** Data persists across container stops and removals. Use `docker compose down -v` to remove volumes and data.

### Health Checks

Docker health checks ensure containers are ready:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s  # Grace period for migrations
```

Check container health:
```bash
docker ps  # Shows "healthy" or "unhealthy" status
docker inspect --format='{{.State.Health.Status}}' taskflow-api
```

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

**Step 1: Create service principal**
```bash
az ad sp create-for-rbac \
  --name "TaskFlowDeployment" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}
```

Save the `appId` and `tenant` from the output.

**Step 2: Configure federated credentials**
```bash
APP_ID=$(az ad app list --display-name "TaskFlowDeployment" --query "[0].appId" -o tsv)

# For QA environment
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsQA",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:nevridge/TaskFlow.Api:environment:qa",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# For production environment
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsProduction",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:nevridge/TaskFlow.Api:environment:production",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

**Step 3: Configure GitHub secrets**

In your GitHub repository, go to **Settings → Secrets and variables → Actions** and add:
- `AZURE_CLIENT_ID` - The `appId` from Step 1
- `AZURE_TENANT_ID` - The `tenant` from Step 1
- `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID

**Step 4: Create GitHub environments**

In **Settings → Environments**, create:
- `qa` - For QA deployments
- `production` - For production deployments (consider adding protection rules)

For detailed OIDC setup instructions, see [AZURE_OIDC_AUTHENTICATION.md](AZURE_OIDC_AUTHENTICATION.md).

### Production Deployment to Azure App Service

The production deployment workflow automatically deploys to Azure App Service.

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
- **App Service Plan:** `nevridge-taskflow-prod-plan` (F1 SKU)
- **Web App:** `nevridge-taskflow-prod-web`
- **Public URL:** `https://nevridge-taskflow-prod-web.azurewebsites.net`

**Workflow steps:**
1. Builds Docker image via Azure Container Registry
2. Creates Azure resources (if they don't exist)
3. Configures managed identity for ACR access
4. Deploys container to Web App
5. Verifies deployment via health check

**Note:** The deployment uses Free tier (F1) by default due to common Azure quota limitations. To use Basic (B1) or higher tiers, request quota increase in Azure Portal.

### QA Deployment to Azure Container Instances

The QA workflow creates ephemeral test environments with predictable DNS names.

**Trigger QA deployment:**
1. Go to **Actions** tab in GitHub
2. Select **Ephemeral ACI deploy - create test teardown** workflow
3. Click **Run workflow**
4. Configure:
   - **action:** `deploy`
   - **resource_group:** Optional (default: `TaskFlowRG`)
   - **location:** Azure region (default: `eastus`)
   - **image_tag:** Docker image tag (default: `latest`)

**QA endpoint:**
- **DNS name:** `taskflow-qa.{region}.azurecontainer.io`
- **Example:** `http://taskflow-qa.eastus.azurecontainer.io:8080`
- **Health check:** `http://taskflow-qa.eastus.azurecontainer.io:8080/health`

The DNS name is fixed and predictable, making it ideal for:
- Postman collections with static QA URLs
- Automated test suites
- Demo environments
- Integration testing

**Teardown QA environment:**
1. Run the same workflow with **action:** `teardown`
2. Specify the resource group name
3. All QA resources will be deleted

For more details on QA deployments, see [QA_DEPLOYMENT.md](QA_DEPLOYMENT.md).

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
- Web App and App Service Plan
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

For complete naming convention details, see [deploy.md](deploy.md).

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
- Deploys to Azure App Service

**QA Deploy:**
- File: `.github/workflows/qa-deploy.yaml`
- Triggers: Manual only
- Environment: `qa`
- Deploys to Azure Container Instances

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

**Quota limitations:**
```
ERROR: Operation cannot be completed without additional quota.
Current limit (Basic VMs): 0
```

Solution: Request quota increase in Azure Portal:
1. Navigate to **Subscriptions** → Your subscription
2. Go to **Usage + quotas**
3. Search for "App Service" or "Basic VMs"
4. Click **Request increase**
5. Submit with justification

**Health check failures post-deployment:**
```bash
# View App Service logs
az webapp log tail --name {WEBAPP_NAME} --resource-group {RESOURCE_GROUP}

# Check application logs
az webapp log download --name {WEBAPP_NAME} --resource-group {RESOURCE_GROUP}
```

**Container deployment issues:**
- Verify ACR access: Check managed identity has `AcrPull` role
- Confirm image exists: `az acr repository show -n {ACR_NAME} --image taskflowapi:latest`
- Check Web App configuration: `az webapp config show --name {WEBAPP_NAME}`

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

For comprehensive logging documentation, see [logging.md](logging.md).

## Additional Resources

- [Docker Configuration Details](DOCKER_CONFIGURATION.md) - Detailed Docker configuration comparison
- [Volume Configuration](volumes.md) - Docker volume management
- [Azure OIDC Authentication](AZURE_OIDC_AUTHENTICATION.md) - Detailed OIDC setup
- [QA Deployment Guide](QA_DEPLOYMENT.md) - Ephemeral QA environment details
- [Resource Naming Convention](deploy.md) - Azure naming standards
- [Security Scanning](SECURITY_SCANNING.md) - CodeQL and Trivy configuration

---

[← Back to README](../README.md) | [Architecture](ARCHITECTURE.md) | [Contributing →](CONTRIBUTING.md)
