# TaskFlow.Api

## Overview
TaskFlow.Api is a small .NET 9 Web API for managing task items (CRUD). The project is intended as a learning playground and a minimal foundation you can extend — it includes persistent storage via EF Core + SQLite, OpenAPI (Swagger), and structured logging with Serilog.

## Key features
- RESTful endpoints for task items (`GET`, `POST`, `PUT`, `DELETE`) exposed under `api/TaskItems`.
- Persistence using Entity Framework Core with SQLite (`TaskDbContext`).
- Migrations generated and tracked in `Migrations/` so schema changes are versioned.
- Structured logging with Serilog (console + rolling file sink).
- Application Insights telemetry integration for monitoring (optional, configured via connection string).
- Configuration-driven connection strings and feature flags via `appsettings.json` and environment variables.
- Swagger/OpenAPI support enabled in Development.

## Prerequisites
- .NET 9 SDK installed.
- Recommended (development): a terminal and optional __Package Manager Console__ or the CLI tools for EF commands.
- **For Docker development**: Docker Desktop installed and running.
- **For Visual Studio Docker support**: Visual Studio 2022 (17.0+) with "Container Development Tools" workload installed.

## Getting started (local development)
1. Clone the repo and change directory:
   - git clone ... && cd TaskFlow.Api
2. Restore packages:
   - `dotnet restore`
3. (Optional) Install required tools if you need to run EF CLI locally:
   - `dotnet tool install --global dotnet-ef` (if not already installed)
4. Apply database migrations (recommended):
   - `dotnet ef database update --project TaskFlow.Api`
   - Alternatively, the app will auto-apply migrations on startup in Development or when the `Database:MigrateOnStartup` flag is enabled (see Configuration below).
5. Run the app:
   - `dotnet run --project TaskFlow.Api`
6. Open the Swagger UI in Development:
   - `https://localhost:{port}/` (the app logs the exact URL on startup)

## Docker deployment
The project includes Docker support for both development and production deployments.

> **📖 For comprehensive Docker configuration details**, including configuration comparisons, troubleshooting, and best practices, see [Docker Configuration Guide](docs/DOCKER_CONFIGURATION.md).

### Local development with Docker

#### Option 1: Visual Studio (Windows/Mac)
If you have Visual Studio 2022 with the "Container Development Tools" workload installed:

1. Open the solution in Visual Studio
2. Select **"Container (Dockerfile.dev)"** from the debug dropdown
3. Press **F5** or click the Run button
4. Visual Studio will automatically:
   - Build the Docker image using Dockerfile.dev
   - Start the container with Development environment
   - Enable automatic database migrations
   - Open your browser to the API

**Requirements:**
- Visual Studio 2022 (17.0 or later)
- "Container Development Tools" workload (install via Visual Studio Installer)
- Docker Desktop running

#### Option 2: Docker Compose (Cross-platform)
For command-line or non-Visual Studio users:

1. **Start the containers**:
   
   **Bash/PowerShell:**
   ```shell
   docker compose up
   ```
   
   The API will be available at `http://localhost:8080`. The development configuration automatically:
   - Runs in Development mode
   - Auto-applies database migrations
   - Persists the SQLite database in a Docker named volume
   - Persists application logs in a Docker named volume

2. **Stop the containers**:
   
   **Bash/PowerShell:**
   ```shell
   docker compose down
   ```
   
   **Note:** This preserves data in volumes. To remove data as well, use `docker compose down -v`.

3. **View logs**:
   
   **Bash/PowerShell:**
   ```shell
   docker compose logs -f
   ```

4. **Test volume persistence** (optional):
   
   See [docs/VOLUME_TESTING.md](docs/VOLUME_TESTING.md) for comprehensive testing instructions to verify data persists across container recreation.

**Docker Compose Services:**
The `docker-compose.yml` defines the following service:
- `taskflow-api`: The main API service
  - **Image**: Built from `Dockerfile.dev`
  - **Ports**: Maps host port `8080` to container port `8080`
  - **Volumes**: 
    - `taskflow-data:/app/data` - Docker named volume for SQLite database
    - `taskflow-logs:/app/logs` - Docker named volume for application logs
  - **Environment Variables**:
    - `ASPNETCORE_ENVIRONMENT=Development` - Runs in development mode
    - `ASPNETCORE_URLS=http://+:8080` - Configures Kestrel to listen on port 8080
    - `ASPNETCORE_HTTP_PORTS=8080` - Sets HTTP port
    - `Database__MigrateOnStartup=true` - Enables automatic database migrations
    - `DOTNET_RUNNING_IN_CONTAINER=true` - Indicates running in container

**Important Notes:**
- Docker named volumes (`taskflow-data` and `taskflow-logs`) are automatically created on first run
- **Data persists** across container removal and recreation - volumes remain until explicitly deleted with `-v` flag
- Database file is stored at `/app/data/tasks.dev.db` (configured in `appsettings.Development.json`)
- Application logs are written to `/app/logs/log.txt`
- Volumes are managed by Docker and stored in Docker's volume directory (typically `/var/lib/docker/volumes/` on Linux)
- To verify persistence works correctly, see [docs/VOLUME_TESTING.md](docs/VOLUME_TESTING.md)
- For detailed volume configuration, management commands, and troubleshooting, see [docs/volumes.md](docs/volumes.md)

#### Option 3: Docker CLI (Alternative)
Using Docker directly without compose:

**Bash/PowerShell:**
```shell
cd TaskFlow.Api
docker build -f Dockerfile.dev -t taskflow-api:dev .
# Create named volumes (optional - Docker will create them automatically if they don't exist)
docker volume create taskflow-data
docker volume create taskflow-logs
# Run container with named volumes
docker run -p 8080:8080 \
  -v taskflow-data:/app/data \
  -v taskflow-logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Database__MigrateOnStartup=true \
  taskflow-api:dev
```

**Note:** Without the `-v` volume options, data and logs will be lost when the container is removed.

### Production deployment

#### Building the Docker image
From the repository root (where `docker-compose.yml` is located):

**Bash/PowerShell:**
```shell
docker build -f TaskFlow.Api/Dockerfile -t taskflow-api:latest .
```

Note: The production `Dockerfile` uses a multi-stage build with the repository root as context.

#### Running the container

**Option 1: Using Docker named volumes (recommended):**

**Bash:**
```bash
# Create named volumes (optional - Docker creates them automatically)
docker volume create taskflow-prod-data
docker volume create taskflow-prod-logs

# Run with Docker named volumes for persistence
docker run -d -p 8080:8080 \
  -v taskflow-prod-data:/app/data \
  -v taskflow-prod-logs:/app/logs \
  --name taskflow-api taskflow-api:latest
```

**PowerShell:**
```powershell
# Create named volumes (optional - Docker creates them automatically)
docker volume create taskflow-prod-data
docker volume create taskflow-prod-logs

# Run with Docker named volumes for persistence
docker run -d -p 8080:8080 `
  -v taskflow-prod-data:/app/data `
  -v taskflow-prod-logs:/app/logs `
  --name taskflow-api taskflow-api:latest
```

**Option 2: Using bind mounts (host directories):**

**Bash:**
```bash
# For persistent storage using host directories
docker run -d -p 8080:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  --name taskflow-api taskflow-api:latest
```

**PowerShell:**
```powershell
# For persistent storage using host directories
docker run -d -p 8080:8080 `
  -v ${PWD}/data:/app/data `
  -v ${PWD}/logs:/app/logs `
  --name taskflow-api taskflow-api:latest
```

**⚠️ Important:** Without volume mounts, all data and logs will be lost when the container is removed.

**Customizing paths:**
The application uses `/app/data/tasks.db` for the database and `/app/logs/log.txt` for logs by default. To use custom paths, override via environment variables:

**Bash:**
```bash
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/production.db" \
  -e LOG_PATH=/app/logs/taskflow.log \
  -v taskflow-prod-data:/app/data \
  -v taskflow-prod-logs:/app/logs \
  --name taskflow-api taskflow-api:latest
```

**PowerShell:**
```powershell
docker run -d -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/production.db" `
  -e LOG_PATH=/app/logs/taskflow.log `
  -v taskflow-prod-data:/app/data `
  -v taskflow-prod-logs:/app/logs `
  --name taskflow-api taskflow-api:latest
```

### Docker configuration summary

#### Dockerfiles
- **Development Dockerfile** (`Dockerfile.dev`):
  - Uses .NET 9 SDK and ASP.NET runtime
  - Build context is the `TaskFlow.Api` directory
  - Sets `ASPNETCORE_ENVIRONMENT=Development`
  - Creates `/app/logs` and `/app/data` directories for persistence
  
- **Production Dockerfile** (`Dockerfile`):
  - Two-stage build: SDK for build, ASP.NET runtime for final image
  - Build context is the repository root
  - Sets `ASPNETCORE_ENVIRONMENT=Production`
  - Creates `/app/logs` and `/app/data` directories for persistence
  - Exposes port 8080

#### Docker Compose Files
- **docker-compose.yml** (Development):
  - Builds using `Dockerfile.dev` from the `TaskFlow.Api` directory
  - Uses Docker named volumes `taskflow-data` and `taskflow-logs` for persistence
  - Volumes persist data across container removal and recreation
  - Configures development environment with automatic migrations
  - Container name: `taskflow-api`
  - Database: `/app/data/tasks.dev.db`

- **docker-compose.prod.yml** (Production):
  - Builds using `Dockerfile` from the repository root
  - Same volume mounts as development
  - Configures production environment with manual migration control
  - Container name: `taskflow-api-prod`
  - Database: `/app/data/tasks.db`

#### Key environment variables
  - `ASPNETCORE_ENVIRONMENT`: Controls environment (Development/Production)
  - `ASPNETCORE_URLS` / `ASPNETCORE_HTTP_PORTS`: Configure Kestrel ports
  - `Database__MigrateOnStartup`: Enable/disable automatic migrations (true/false)
  - `ConnectionStrings__DefaultConnection`: Override SQLite database path (default: `/app/data/tasks.db`)
  - `LOG_PATH`: Override log file path (default: `/app/logs/log.txt`)
  - `DOTNET_RUNNING_IN_CONTAINER`: Signals container runtime

#### Configuration differences explained
The intentional differences between development and production configurations are:
- **Build context**: Development optimizes for rapid iteration, production supports multi-project solutions
- **Environment variables**: Development enables Swagger and verbose logging, production uses optimized settings
- **Migration strategy**: Development auto-applies migrations, production requires explicit control
- **Database files**: Separate database files prevent mixing dev and production-like test data

For comprehensive configuration details, comparisons, and troubleshooting, see [Docker Configuration Guide](docs/DOCKER_CONFIGURATION.md).

For detailed volume configuration, see [docs/volumes.md](docs/volumes.md).

### Docker notes
- The `.dockerignore` file excludes build artifacts, dependencies, and unnecessary files from the build context
- The container exposes port 8080 by default
- **Data Persistence:** Docker volumes ensure SQLite database and logs persist across container removal and recreation
- **Volume Management:** Use `docker volume ls` to list volumes, `docker volume rm` to remove them, and `docker volume inspect` to view details
- **⚠️ Persistence warning:** Without volume mounting, the SQLite database and logs will be lost when the container is removed
- By default, the production Docker container runs in Production mode and migrations will **not** auto-apply. To enable automatic migrations, set either `ASPNETCORE_ENVIRONMENT=Development` or `Database__MigrateOnStartup=true` via environment variables when running the container.

## Azure deployment

The project includes automated deployment workflows for Azure App Service using GitHub Actions. Two deployment options are available:

### Production deployment to Azure App Service

The production deployment workflow (`.github/workflows/deploy.yaml`) automatically deploys to Azure when you push a tag or manually trigger the workflow.

#### Prerequisites
1. **Azure subscription** with permissions to create resources
2. **Azure service principal** with Contributor access to your subscription
3. **GitHub repository secrets** for OIDC authentication:
   - `AZURE_CLIENT_ID` - Application (client) ID
   - `AZURE_TENANT_ID` - Directory (tenant) ID
   - `AZURE_SUBSCRIPTION_ID` - Subscription ID

#### Creating the Azure service principal with OIDC authentication

The application uses OpenID Connect (OIDC) for secure, passwordless authentication to Azure. This modern approach eliminates the need to store and rotate credentials.

**Step 1: Create the service principal**

**Bash:**
```bash
az ad sp create-for-rbac \
  --name "TaskFlowDeployment" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}
```

**PowerShell:**
```powershell
az ad sp create-for-rbac `
  --name "TaskFlowDeployment" `
  --role contributor `
  --scopes /subscriptions/{subscription-id}
```

Save the output - you'll need the `appId` (Client ID) and `tenant` (Tenant ID).

**Step 2: Configure federated credentials**

Configure GitHub Actions to authenticate using OIDC:

**Note**: The Azure CLI (`az`) commands below work cross-platform on Windows (PowerShell/CMD), macOS, and Linux.

```bash
APP_ID=$(az ad app list --display-name "TaskFlowDeployment" --query "[0].appId" -o tsv)

# For main branch deployments
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsMain",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:nevridge/TaskFlow.Api:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# For tag-based releases
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsTags",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:nevridge/TaskFlow.Api:ref:refs/tags/v*",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

**Step 3: Configure GitHub secrets**

Add these secrets in GitHub (**Settings → Secrets and variables → Actions**):
- `AZURE_CLIENT_ID` - The `appId` from Step 1
- `AZURE_TENANT_ID` - The `tenant` from Step 1
- `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID

**For detailed setup instructions and troubleshooting, see [Azure OIDC Authentication Guide](docs/AZURE_OIDC_AUTHENTICATION.md).**

#### Deployment workflow configuration

The workflow uses a **standardized naming convention** for Azure resources. All resource names are computed automatically based on organization, application, and environment identifiers. See [docs/deploy.md](docs/deploy.md) for complete details.

**Default production resource names** (in `.github/workflows/deploy.yaml`):
- **Organization**: `nevridge`
- **Application**: `taskflow`
- **Environment**: `prod`
- **Resource Group**: `nevridge-taskflow-prod-rg` (location: `eastus`)
- **Azure Container Registry (ACR)**: `nevridgetaskflowprodacr`
- **App Service Plan**: `nevridge-taskflow-prod-plan` (Linux, B1 SKU)
- **Web App**: `nevridge-taskflow-prod-web`
- **ACR Image**: `taskflowapi`

**Public URL**: `https://nevridge-taskflow-prod-web.azurewebsites.net`

To customize, modify the `ORG_NAME`, `APP_NAME`, or `ENV` variables at the top of the workflow file. For details on the naming convention and validation rules, see the [Resource Naming Convention Guide](docs/deploy.md).

#### Triggering a deployment

**Option 1: Tag-based deployment (recommended)**

**Bash/PowerShell:**
```shell
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

**Option 2: Manual deployment**
1. Go to **Actions** tab in GitHub
2. Select **Deploy to Azure Production** workflow
3. Click **Run workflow** button
4. Select the branch and click **Run workflow**

#### What the workflow does

1. **Builds and pushes Docker image** to Azure Container Registry using `az acr build`
2. **Creates Azure resources** if they don't exist:
   - Resource Group
   - Azure Container Registry (Basic SKU)
   - App Service Plan (Linux B1)
   - Web App with Linux container
3. **Configures managed identity**: Enables system-assigned managed identity for the Web App
4. **Grants ACR access**: Assigns AcrPull role to the Web App identity
5. **Deploys container**: Updates the Web App with the latest image
6. **Verifies deployment**: Checks the `/health` endpoint for successful startup

#### Post-deployment

After successful deployment, your API will be available at:
```
https://{WEBAPP_NAME}.azurewebsites.net
```

For example: `https://taskflowapi2074394909.azurewebsites.net`

The workflow automatically verifies the deployment by checking the health endpoint.

### Ephemeral deployment to Azure Container Instances (ACI)

The ephemeral deployment workflow (`.github/workflows/ephemeral-deploy.yaml`) creates a QA test environment using Azure Container Instances with a **fixed, predictable DNS name**.

#### Use cases
- QA testing with a stable endpoint
- Testing pull requests in an isolated environment
- Integration testing and automated test suites
- Demo environments with consistent URLs

#### Fixed QA DNS endpoint

The QA deployment always uses the same DNS name for predictability:
- **DNS Name Label**: `taskflow-qa`
- **Full DNS Format**: `taskflow-qa.{region}.azurecontainer.io`
- **Example (eastus region)**: `taskflow-qa.eastus.azurecontainer.io`
- **API Endpoint**: `http://taskflow-qa.{region}.azurecontainer.io:8080`
- **Health Check**: `http://taskflow-qa.{region}.azurecontainer.io:8080/health`

This fixed DNS allows you to:
- Configure Postman collections with a static QA URL
- Set up automated tests without updating endpoints
- Share a consistent QA link with team members
- Avoid DNS changes between deployments

#### How it works

The workflow automatically manages DNS conflicts:
1. **Checks for existing container**: Before deploying, checks if a container with the name `taskflow-qa` exists
2. **Replaces previous deployment**: If found, deletes the old container to free up the DNS name
3. **Creates new container**: Deploys the new version with the same fixed DNS name `taskflow-qa`
4. **Verifies deployment**: Performs a smoke test on the `/health` endpoint

This ensures the QA DNS name remains constant across all deployments while always serving the latest version.

#### Triggering ephemeral deployment

This workflow must be triggered manually with parameters:

1. Go to **Actions** tab in GitHub
2. Select **Ephemeral ACI deploy - create test teardown** workflow
3. Click **Run workflow**
4. Configure parameters:
   - **action**: `deploy` (to create) or `teardown` (to delete)
   - **resource_group**: Optional resource group name (default: `TaskFlowRG`)
   - **location**: Azure region (default: `eastus`)
   - **acr_name**: Optional ACR name (default: `taskflowregistry`)
   - **image_tag**: Docker image tag (default: `latest`)

#### Deploy workflow steps
1. Builds Docker image and pushes to ACR
2. Deletes any existing ACI container with the name `taskflow-qa` (if present)
3. Creates new Azure Container Instance with fixed DNS name `taskflow-qa`
4. Exposes API on the predictable FQDN: `taskflow-qa.{region}.azurecontainer.io`
5. Performs smoke test on `/health` endpoint
6. Displays the fixed QA endpoint URL in the workflow output

#### Using the QA endpoint in Postman

To configure Postman for the QA environment:

1. **Create/Update an Environment** in Postman named "TaskFlow QA"
2. **Set the base URL variable**:
   - Variable name: `baseUrl`
   - Value: `http://taskflow-qa.eastus.azurecontainer.io:8080` (adjust region if different)
3. **Use the variable in requests**: `{{baseUrl}}/api/TaskItems`

The QA endpoint URL never changes, so you set it once and use it for all future QA testing.

> For troubleshooting, advanced testing, and more details on QA deployments, see the [QA Deployment Guide](docs/QA_DEPLOYMENT.md).
#### Using the QA endpoint in automated tests

Example test configuration:

**JavaScript/Node.js:**
```javascript
// test-config.js
const QA_BASE_URL = 'http://taskflow-qa.eastus.azurecontainer.io:8080';

// Use in tests
describe('TaskFlow API QA Tests', () => {
  it('should return healthy status', async () => {
    const response = await fetch(`${QA_BASE_URL}/health`);
    expect(response.status).toBe(200);
  });
});
```

**Python:**
```python
# test_config.py
QA_BASE_URL = 'http://taskflow-qa.eastus.azurecontainer.io:8080'

# Use in tests
def test_health_endpoint():
    response = requests.get(f'{QA_BASE_URL}/health')
    assert response.status_code == 200
```

**Bash/cURL:**
```bash
# qa-test.sh
QA_URL="http://taskflow-qa.eastus.azurecontainer.io:8080"
curl -f "$QA_URL/health" || exit 1
```

#### Teardown
To delete the ephemeral environment:
1. Run the workflow with `action: teardown`
2. Provide the `resource_group` name from the deployment
3. The workflow will delete the entire resource group including the QA container

**Note**: Ephemeral deployments use the same ACR as production by default. You can specify a different ACR name if needed.

#### Region-specific DNS

If you deploy to different Azure regions, the DNS name will vary by region:
- **East US**: `taskflow-qa.eastus.azurecontainer.io`
- **West US**: `taskflow-qa.westus.azurecontainer.io`
- **West Europe**: `taskflow-qa.westeurope.azurecontainer.io`

Update your Postman environments and test configurations to match the region you deploy to.

### Production teardown

A separate workflow is available to tear down production Azure resources. This is useful for cleaning up after testing or when you want to completely remove the production deployment to avoid costs.

**File**: `.github/workflows/production-teardown.yaml`

#### Triggering production teardown

⚠️ **WARNING**: This action is destructive and will permanently delete all production resources and data.

1. Go to **Actions** tab in GitHub
2. Select **Production Teardown** workflow
3. Click **Run workflow**
4. **Type "CONFIRM"** in the confirmation field (case-sensitive)
5. Click **Run workflow**

#### What gets deleted

The workflow will delete the entire production resource group, which includes:
- **Web App**: `nevridge-taskflow-prod-web`
- **App Service Plan**: `nevridge-taskflow-prod-plan`
- **Container Registry**: `nevridgetaskflowprodacr`
- **All associated data, logs, and configurations**

The deletion runs asynchronously in Azure. You can monitor progress via:

**Bash/PowerShell:**
```shell
az group show -n nevridge-taskflow-prod-rg
```

Once the deletion completes, the command will return an error indicating the resource group no longer exists.

#### Safety features

The production teardown workflow includes several safety measures:
1. **Separate workflow file**: Isolated from deployment workflow to prevent accidental execution
2. **Manual trigger only**: Cannot be triggered automatically; requires explicit user action
3. **Confirmation required**: Must type "CONFIRM" exactly to proceed
4. **Production environment**: Uses GitHub environment protection if configured
5. **Clear logging**: Displays all resources that will be deleted before proceeding

#### Re-deploying after teardown

After tearing down production resources, you can redeploy by:
1. Pushing a new tag (e.g., `git tag v1.0.1 && git push origin v1.0.1`)
2. Or manually triggering the **Deploy to Azure Production** workflow

The deployment workflow will recreate all necessary resources automatically.

### Azure deployment best practices

1. **Resource naming**: Customize resource names in workflow files before first deployment
2. **Secrets management**: Never commit `AZURE_CREDENTIALS` to source control
3. **Database persistence**: Azure App Service's `/home` directory is persistent across restarts
4. **Monitoring**: Enable Application Insights for production deployments
5. **Scaling**: Adjust App Service Plan SKU based on your traffic requirements
6. **Cost optimization**: Use ephemeral deployments for testing to minimize costs

### Troubleshooting Azure deployments

If deployment fails:
1. **Check workflow logs** in GitHub Actions for detailed error messages
2. **Verify Azure credentials**: Ensure OIDC secrets (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`) are configured correctly and the service principal has proper permissions
3. **Check resource quotas**: Ensure your subscription has available quota
4. **Health check failures**: Check App Service logs in Azure Portal
5. **Container logs**: View logs via Azure CLI:
   
   **Bash/PowerShell:**
   ```shell
   az webapp log tail --name {WEBAPP_NAME} --resource-group {RESOURCE_GROUP}
   ```

## CI/CD workflows

The project includes GitHub Actions workflows for continuous integration, deployment, and security scanning:

### Build and Test Workflow
**File**: `.github/workflows/ci.yml`

**Purpose**: Continuous integration pipeline that builds, tests, and enforces code quality standards

**Triggers**:
- Push to any branch
- Pull requests

**Jobs**:
1. **lint**: Validates C# code formatting using `dotnet format`
2. **build**: Compiles the solution in Release configuration
3. **test**: Runs unit tests with code coverage and enforces minimum coverage threshold

**Code Coverage Gating**:
The test job enforces a minimum line coverage threshold of 58% using Coverlet. The build will fail if coverage drops below this threshold, ensuring code quality is maintained. The workflow generates and displays a detailed coverage report including:
- Line coverage percentage
- Branch coverage percentage  
- Method coverage percentage
- Per-class coverage breakdown

**Key features**:
- Uses Coverlet for code coverage collection
- Enforces minimum 58% line coverage threshold
- Generates detailed coverage reports with ReportGenerator
- Excludes test frameworks, Program.cs, and middleware from coverage calculations
- Coverage threshold can be incrementally increased as test coverage improves

### Security Scanning Workflows

The repository includes automated security scanning to identify vulnerabilities:

#### Workflow: CodeQL Security Analysis
**File**: `.github/workflows/codeql.yml`

**Purpose**: Static analysis of C# code to identify security vulnerabilities and code quality issues

**Triggers**:
- Push to `main` branch
- Pull requests to `main`
- Weekly schedule (Sundays at 4am UTC)

**Key features**:
- Buildless scanning (no compilation required for .NET 9)
- Supports C# 12 and .NET 9
- Results appear in GitHub Security > Code scanning alerts
- Identifies SQL injection, XSS, and other security vulnerabilities

#### Workflow: Docker Security Scan (Trivy)
**File**: `.github/workflows/security-scan.yml`

**Purpose**: Scans Docker images for OS and library vulnerabilities

**Triggers**:
- Push to `main` branch
- Pull requests to `main`
- Weekly schedule (Sundays at 5am UTC)

**Key features**:
- Scans for CRITICAL, HIGH, and MEDIUM severity vulnerabilities
- Fails build on CRITICAL/HIGH findings
- Uploads SARIF results to GitHub Security tab
- Stores detailed scan reports as artifacts (30-day retention)

**For detailed information**, see [docs/SECURITY_SCANNING.md](docs/SECURITY_SCANNING.md)

### Deployment Workflows

The project includes three GitHub Actions workflows for continuous deployment and teardown:

### Workflow: Deploy to Azure Production
**File**: `.github/workflows/deploy.yaml`

**Triggers**:
- Manual trigger via workflow_dispatch
- Automatic trigger on tags matching `v*` (e.g., `v1.0.0`, `v2.1.3`)

**Required secrets**:
- `AZURE_CLIENT_ID`: Application (client) ID
- `AZURE_TENANT_ID`: Directory (tenant) ID
- `AZURE_SUBSCRIPTION_ID`: Subscription ID

**Key features**:
- Builds Docker image using Azure Container Registry build (no local Docker needed)
- Creates/updates all required Azure resources automatically
- Uses managed identity for secure ACR access (no passwords stored)
- Performs health check verification after deployment
- Supports idempotent deployments (safe to run multiple times)

**Environment**: `production` (configure in GitHub repository settings)

### Workflow: Ephemeral ACI deploy - create test teardown
**File**: `.github/workflows/ephemeral-deploy.yaml`

**Triggers**:
- Manual trigger only via workflow_dispatch with parameters

**Required secrets**:
- `AZURE_CLIENT_ID`: Application (client) ID
- `AZURE_TENANT_ID`: Directory (tenant) ID
- `AZURE_SUBSCRIPTION_ID`: Subscription ID

**Input parameters**:
- `action`: `deploy` or `teardown`
- `resource_group`: Resource group name (optional, uses default if not provided)
- `location`: Azure region (default: `eastus`)
- `acr_name`: ACR name (optional, uses default if not provided)
- `image_tag`: Docker image tag (default: `latest`)

**Key features**:
- Creates isolated test environments with unique DNS names
- Supports both deployment and cleanup operations
- Uses basic authentication for ACR (suitable for ephemeral scenarios)
- Performs smoke testing on deployed instances
- Generates unique resource names using GitHub run ID

### Workflow: Production Teardown
**File**: `.github/workflows/production-teardown.yaml`

**Triggers**:
- Manual trigger only via workflow_dispatch with confirmation

**Required secrets**:
- `AZURE_CREDENTIALS`: Service principal JSON with subscription access

**Input parameters**:
- `confirm`: Must type "CONFIRM" exactly to proceed with teardown

**Key features**:
- Safely deletes all production Azure resources
- Requires explicit confirmation to prevent accidental execution
- Separate workflow file for safety
- Uses production environment protection (if configured)
- Displays all resources to be deleted before proceeding
- Runs deletion asynchronously in Azure

**See also**: [Production Teardown](#production-teardown) section above for detailed usage instructions.

### Modifying workflows

To customize deployment for your environment:

1. **Update resource names** in workflow `env` section:
   ```yaml
   env:
     RESOURCE_GROUP: YourResourceGroup
     WEBAPP_NAME: your-webapp-name
     ACR_NAME: yourregistryname
     # ... other variables
   ```

2. **Change Azure region**: Update `LOCATION` environment variable

3. **Adjust App Service SKU**: Modify the `--sku` parameter in the App Service Plan creation step
   - `F1`: Free tier (limited, not for production)
   - `B1`: Basic (suitable for small production workloads)
   - `S1`: Standard (production with scaling)
   - `P1V2`: Premium (high performance)

4. **Add deployment slots**: Add steps to create and use deployment slots for zero-downtime deployments

5. **Environment-specific configuration**: Use GitHub environments to store different secrets per environment

### Workflow best practices

1. **Use tags for releases**: Tag-based deployments provide clear version tracking
2. **Test in ephemeral first**: Use ACI workflow to test changes before production
3. **Monitor workflow runs**: Set up GitHub notifications for workflow failures
4. **Secure secrets**: Regularly rotate service principal credentials
5. **Branch protection**: Require pull request reviews before merging to main
6. **Status badges**: Add workflow status badges to README for visibility

## Health checks

The API includes comprehensive health check endpoints for monitoring and container orchestration.

### Health check endpoints

#### 1. Overall health check: `/health`
- **Purpose**: Returns the overall health status of the application including all registered health checks
- **Checks**: Database connectivity and self-check (application running)
- **Response**: HTTP 200 (Healthy) or HTTP 503 (Unhealthy)
- **Response format**: JSON with detailed status, duration metrics, and individual check results
- **Use case**: General health monitoring, load balancer health checks

**Example request**:

**Bash:**
```bash
curl http://localhost:8080/health
```

**PowerShell:**
```powershell
Invoke-RestMethod -Uri http://localhost:8080/health
```

**Healthy response** (HTTP 200):
```json
{
  "status": "Healthy",
  "totalDuration": 25.4551,
  "results": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "",
      "duration": 20.355,
      "exception": null,
      "data": null
    },
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running",
      "duration": 1.0933,
      "exception": null,
      "data": null
    }
  ]
}
```

**Unhealthy response** (HTTP 503):
```json
{
  "status": "Unhealthy",
  "totalDuration": 5032.1234,
  "results": [
    {
      "name": "database",
      "status": "Unhealthy",
      "description": "Database connection failed",
      "duration": 5000.5678,
      "exception": "Unable to connect to database",
      "data": null
    },
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running",
      "duration": 0.0234,
      "exception": null,
      "data": null
    }
  ]
}
```

#### 2. Readiness check: `/health/ready`
- **Purpose**: Indicates if the application is ready to receive traffic
- **Checks**: Database connectivity (tagged with "ready")
- **Response**: HTTP 200 (Ready) or HTTP 503 (Not ready)
- **Response format**: JSON with detailed status including only readiness checks
- **Use case**: Kubernetes readiness probes, load balancer registration

**Example request**:

**Bash:**
```bash
curl http://localhost:8080/health/ready
```

**PowerShell:**
```powershell
Invoke-RestMethod -Uri http://localhost:8080/health/ready
```

**Ready response** (HTTP 200):
```json
{
  "status": "Healthy",
  "totalDuration": 43.2066,
  "results": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "",
      "duration": 40.8122,
      "exception": null,
      "data": null
    }
  ]
}
```

**Not ready response** (HTTP 503):
```json
{
  "status": "Unhealthy",
  "totalDuration": 5001.234,
  "results": [
    {
      "name": "database",
      "status": "Unhealthy",
      "description": "Database connection failed",
      "duration": 5000.123,
      "exception": "Unable to open connection",
      "data": null
    }
  ]
}
```

#### 3. Liveness check: `/health/live`
- **Purpose**: Indicates if the application is running and not deadlocked
- **Checks**: Self-check (tagged with "live") - verifies application process is responsive
- **Response**: HTTP 200 (Alive) or HTTP 503 (Dead)
- **Response format**: JSON with detailed status including only liveness checks
- **Use case**: Kubernetes liveness probes, restart decisions

**Example request**:

**Bash:**
```bash
curl http://localhost:8080/health/live
```

**PowerShell:**
```powershell
Invoke-RestMethod -Uri http://localhost:8080/health/live
```

**Alive response** (HTTP 200):
```json
{
  "status": "Healthy",
  "totalDuration": 0.1649,
  "results": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running",
      "duration": 0.0911,
      "exception": null,
      "data": null
    }
  ]
}
```

**Note**: The liveness check is lightweight and does not depend on external services like the database. This prevents restart loops caused by transient infrastructure issues. If the liveness check fails, the application is truly deadlocked and should be restarted.

### Health check implementation

The health checks are configured in `Program.cs`:

```csharp
// Register health checks with proper tagging
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>(
        name: "database",
        tags: new[] { "ready" })
    .AddCheck(
        name: "self",
        check: () => HealthCheckResult.Healthy("Application is running"),
        tags: new[] { "live" });

// Map health check endpoints with custom JSON response writer
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
});
```

**Key implementation details:**

1. **Tagged checks**: Health checks are tagged with "ready" or "live" to allow filtering for specific probe types
2. **Database check**: Tagged with "ready" - verifies database connectivity before accepting traffic
3. **Self-check**: Tagged with "live" - lightweight check that confirms the application process is responsive
4. **Custom response writer**: Provides detailed JSON responses with status, duration, and individual check results
5. **Status codes**: Returns HTTP 200 for healthy, HTTP 503 for unhealthy (automatically handled by ASP.NET Core)

### Configuring health checks for container orchestrators

#### Why initial delays are important

When TaskFlow.Api starts, it automatically applies database migrations (when `Database__MigrateOnStartup=true` or in Development mode). Depending on the database state and migration complexity, this process can take several seconds to complete. If health checks are configured too aggressively (without sufficient initial delay), orchestrators may mark the service as unhealthy and restart the container before migrations finish, causing:
- Failed startups
- Restart loops
- Migration interruptions
- Service unavailability

**Recommended initial delay values:**
- **Docker Compose**: `start_period: 40s` - Allows migrations to complete before health checks begin affecting container status
- **Kubernetes**: `initialDelaySeconds: 45-60s` for readiness probe, `60s` for liveness probe
- **Azure App Service**: Built-in startup grace period, but verify with health check endpoint monitoring

These values are conservative to accommodate typical SQLite migrations. Adjust based on your specific migration complexity and database performance.

#### Docker Compose health check

The `docker-compose.yml` includes a pre-configured healthcheck with appropriate startup delays:

```yaml
services:
  taskflow-api:
    # ... other configuration
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s        # Check every 30 seconds after start_period
      timeout: 10s         # Allow 10 seconds for health check to respond
      retries: 3           # Mark unhealthy after 3 consecutive failures
      start_period: 40s    # Grace period for startup and migrations
```

**Note**: The health check runs inside the container using `curl`, which is included in the ASP.NET base images. If using a minimal base image, you may need to install `curl` or use an alternative health check method.

The `start_period` parameter is critical: it gives the container 40 seconds to complete startup and migrations before health check failures count toward the `retries` limit. During this period, failed health checks won't mark the container as unhealthy.

#### Kubernetes probes

An example Kubernetes deployment manifest with appropriate probe configurations is provided in `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: taskflow-api
        image: taskflow-api:latest
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 60    # Wait 60s before first liveness check
          periodSeconds: 10           # Check every 10 seconds
          timeoutSeconds: 5           # Timeout after 5 seconds
          failureThreshold: 3         # Restart after 3 failures
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 45    # Wait 45s before first readiness check
          periodSeconds: 5            # Check every 5 seconds
          timeoutSeconds: 3           # Timeout after 3 seconds
          failureThreshold: 3         # Remove from service after 3 failures
```

**Key settings explained:**
- **Liveness probe** (`initialDelaySeconds: 60s`): Waits a full minute before checking if the application is alive. This prevents Kubernetes from restarting the pod during migration. Uses `/health/live` which doesn't check database connectivity.
- **Readiness probe** (`initialDelaySeconds: 45s`): Waits 45 seconds before checking if the application is ready to serve traffic. Uses `/health/ready` which includes database connectivity validation.
- The liveness delay is longer than readiness because we want to ensure the app is fully initialized before considering restart decisions.

For production deployments, test with your actual migration scenarios and adjust timing as needed.

#### Azure App Service health check

Azure App Service health checks are configured in the deployment workflow (`.github/workflows/deploy.yaml`):

```bash
az webapp config set \
  --resource-group $RESOURCE_GROUP \
  --name $WEBAPP_NAME \
  --generic-configurations '{"healthCheckPath": "/health"}'
```

**Azure App Service specifics:**
- Azure automatically provides a startup grace period before enforcing health checks
- Health checks ping the configured path (`/health`) at regular intervals
- Failed health checks can trigger automatic instance recycling
- The deployment workflow includes a verification step that waits 30 seconds after restart, then retries health checks for up to 50 seconds (10 attempts × 5s interval)

To configure manually via Azure CLI:

**Bash:**
```bash
az webapp config set \
  --resource-group TaskFlowRG \
  --name taskflowapi2074394909 \
  --generic-configurations '{"healthCheckPath": "/health"}'
```

**PowerShell:**
```powershell
az webapp config set `
  --resource-group TaskFlowRG `
  --name taskflowapi2074394909 `
  --generic-configurations '{\"healthCheckPath\": \"/health\"}'
```

Or configure via Azure Portal:
1. Navigate to your App Service
2. Go to **Health check** under Monitoring
3. Enable health check and set path to `/health`

### Health check best practices

1. **Allow for migrations**: Always configure `initialDelaySeconds` (Kubernetes) or `start_period` (Docker) sufficient for database migrations to complete
2. **Separate concerns**: Use `/health/live` for process health (lightweight), `/health/ready` for dependencies (includes database)
3. **Fail fast for checks**: Individual health checks should complete quickly (< 5 seconds), but initial delays should be generous
4. **Database checks**: Only include database in readiness, not liveness, to avoid restart loops from transient DB issues
5. **Test with clean databases**: Simulate worst-case startup scenarios with empty databases requiring full migration runs
6. **Monitoring**: Set up alerts on repeated health check failures to detect persistent issues
7. **Logging**: Health check failures are automatically logged by Serilog with error/warning severity for troubleshooting. See [Logging Guide](docs/logging.md#health-check-failure-logging) for details on finding and interpreting health check logs

### Response format details

All health check endpoints return JSON with the following structure:

```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "totalDuration": 25.4551,
  "results": [
    {
      "name": "check-name",
      "status": "Healthy|Degraded|Unhealthy",
      "description": "Human-readable description",
      "duration": 20.355,
      "exception": "Error message if failed",
      "data": { "key": "value" }
    }
  ]
}
```

**Field descriptions:**
- **status**: Overall health status aggregated from all checks
- **totalDuration**: Total time in milliseconds to execute all checks
- **results**: Array of individual check results
  - **name**: Unique identifier for the check
  - **status**: Health status of this specific check
  - **description**: Optional human-readable description
  - **duration**: Time in milliseconds for this specific check
  - **exception**: Error message if the check failed (null if successful)
  - **data**: Optional additional data provided by the check (null if none)

### Extending health checks

To add custom health checks, modify the health check registration in `Program.cs`:

```csharp
// Example: Add custom checks for external dependencies
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>(
        name: "database",
        tags: new[] { "ready" })
    .AddCheck(
        name: "self",
        check: () => HealthCheckResult.Healthy("Application is running"),
        tags: new[] { "live" })
    .AddUrlGroup(
        new Uri("https://api.external.com/status"), 
        name: "external-api", 
        tags: new[] { "ready" })
    .AddCheck(
        name: "disk-space",
        check: () => {
            var drive = new DriveInfo("/app/data");
            var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            return freeSpaceGB > 1 
                ? HealthCheckResult.Healthy($"{freeSpaceGB:F2} GB free")
                : HealthCheckResult.Unhealthy($"Low disk space: {freeSpaceGB:F2} GB free");
        },
        tags: new[] { "ready" });
```

**Tag guidelines:**
- Use `"ready"` tag for checks that must pass before accepting traffic (database, external APIs, disk space)
- Use `"live"` tag for lightweight checks that verify the process is responsive (self-check, memory check)
- Checks can have multiple tags if needed
- Liveness checks should complete in < 1 second
- Readiness checks can take longer but should complete in < 5 seconds

## Testing
- Use the built-in Swagger UI to exercise the API in Development.
- Postman collection: import the collection and environment to run example requests and quickly test CRUD flows.
  - Collection (shared): https://studyplan-9664.postman.co/workspace/StudyPlan~b854a959-3425-41a8-9125-d9e7335da054/collection/102031-e46c6909-f827-46a6-affb-06cae2c01a09?action=share&creator=102031&active-environment=102031-85338ffd-20d0-49b1-b9fc-c0008742e5a4
  - Import instructions:
    1. Open Postman → __File > Import__ → paste the collection URL or use the workspace import.
    2. Also import or select the linked environment (the shared workspace should contain it) to ensure base URL and any variables are set.
    3. Adjust the `baseUrl` (if present) or run against `https://localhost:{port}` while the app is running.

## Configuration
- Application settings live in `appsettings.json` (with environment overrides like `appsettings.Development.json`).
- Connection string key: `ConnectionStrings:DefaultConnection`.
  - Example default: `Data Source=tasks.db`
  - Override via environment variable: `ConnectionStrings__DefaultConnection`
- Automatic migration control: `Database:MigrateOnStartup` (boolean). By default the project will only auto-migrate in `Development`. Set `Database__MigrateOnStartup=true` to opt in on other environments (not recommended for production).
- Application Insights: `ApplicationInsights:ConnectionString` (optional). Leave empty for local development or when you don't want to send telemetry. See the Application Insights section for details.

## Database & migrations
- Migration C# files under `Migrations/` must be kept in source control — they define your schema.
- Recommended production workflow: run migrations during deployment (CI/CD or a separate migration step) instead of automatic on-start to avoid concurrent-deployment issues.
- Common EF CLI commands:
  - Add migration: `dotnet ef migrations add <Name> --project TaskFlow.Api`
  - Update database: `dotnet ef database update --project TaskFlow.Api`

## Logging
- Serilog is configured with a minimal bootstrap logger and used as the host logger.
- By default logs are written to console and to daily rolling files at `/app/logs/log.txt` (in containers) or `logs/log.txt` (local development).
- The log path can be customized via the `LOG_PATH` environment variable.
- Log configuration can be extended in `appsettings.json` (Serilog section) and via environment variables.
- **Health check failures** are automatically logged with error/warning severity for troubleshooting.
- Ensure `logs/` is ignored in Git (see .gitignore recommendations below).
- For Docker volume configuration, see [docs/volumes.md](docs/volumes.md).
- **📖 For comprehensive logging documentation**, including health check failure troubleshooting, log aggregation, and alerting setup, see [docs/logging.md](docs/logging.md).

## Application Insights

Application Insights is integrated for monitoring and telemetry. It provides insights into application performance, dependencies, exceptions, and user behavior.

### Configuration

Configure Application Insights by setting the connection string in `appsettings.json` or via environment variable:

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
  }
}
```

**Environment variable:**
```bash
export ApplicationInsights__ConnectionString="InstrumentationKey=..."
```

**Docker:**
```bash
docker run -e ApplicationInsights__ConnectionString="InstrumentationKey=..." ...
```

### Features enabled

The following telemetry is automatically collected:
- **HTTP requests**: All API requests with response times, status codes, and URLs
- **Dependencies**: Database queries (SQLite), external HTTP calls
- **Exceptions**: Unhandled exceptions with stack traces
- **Performance counters**: CPU, memory usage (when available)
- **Event counters**: .NET runtime metrics
- **Adaptive sampling**: Automatically enabled to control data volume and costs

### Cost control

**Zero cost when not configured**: Application Insights only charges for telemetry data ingested. If you don't provide a connection string, no data is sent and no costs are incurred.

**Adaptive sampling**: Enabled by default. It automatically reduces telemetry volume during high-traffic periods while preserving statistical accuracy. This keeps costs predictable.

**Sampling rate**: The default adaptive sampling targets 5 items/second. For most small to medium applications, this results in minimal costs (often within free tier limits of 5GB/month).

**No infrastructure charges**: Application Insights resources in Azure don't have infrastructure costs—you only pay for data ingested and retained.

### Setting up Application Insights in Azure

1. **Create Application Insights resource** in Azure Portal:
   - Navigate to **Create a resource** → **Application Insights**
   - Choose a name, resource group, and region
   - Select workspace-based mode (recommended)

2. **Get the connection string**:
   - In the Application Insights resource, go to **Overview**
   - Copy the **Connection String** (not just the Instrumentation Key)

3. **Configure the application**:
   - Add the connection string to `appsettings.json` or as an environment variable
   - Restart the application

4. **Verify telemetry**:
   - Open the Application Insights resource in Azure Portal
   - Navigate to **Live Metrics** to see real-time telemetry
   - Check **Transaction search** for recent requests

### Local development

For local development, you have several options:

1. **No configuration** (recommended): Leave the connection string empty. The application will run normally without sending telemetry.

2. **Use a development Application Insights resource**: Create a separate Application Insights resource for development to avoid mixing dev and production data.

3. **Use local Application Insights emulator**: Not officially supported, but you can use connection string from a dev resource for testing.

### Teardown and cost management

**To stop all charges**:
1. Remove the connection string from configuration (or set to empty string)
2. Restart the application
3. (Optional) Delete the Application Insights resource in Azure Portal to ensure no future charges

**To reduce costs without removing**:
- Adaptive sampling is already enabled by default
- Configure shorter data retention (default is 90 days, minimum is 30 days)
- Set daily cap in Application Insights resource (under **Usage and estimated costs**)

**Monitoring costs**:
- View costs in Azure Portal under **Cost Management**
- Check ingestion volume in Application Insights → **Usage and estimated costs**
- The first 5 GB/month is free (check current pricing at [Azure Pricing page](https://azure.microsoft.com/en-us/pricing/details/monitor/); pricing may change)

### Disabling Application Insights

To completely disable Application Insights:

1. **Remove or comment out** the service registration in `Program.cs`:
   ```csharp
   // builder.Services.AddApplicationInsights();
   ```

2. **Remove the NuGet package** (optional):
   ```bash
   dotnet remove package Microsoft.ApplicationInsights.AspNetCore
   ```

## Security & production notes
- Do not commit local runtime artifacts such as the SQLite DB file or log files.
- Prefer explicit migration runs in CI/CD for production deployments.
- Protect any secrets (connection strings for real DBs) via environment variables, Azure Key Vault, or user secrets — do not store production secrets in `appsettings.json`.

## Git / .gitignore recommendations
- Commit source files and migration files.
- Ignore runtime artifacts:
  - `logs/`
  - `*.db`, `*.sqlite`, `*.sqlite3`, and DB journal files `*-wal`, `*-shm`
- If `tasks.db` was accidentally committed, remove it from tracking but keep it locally:
  
  **Bash/PowerShell:**
  ```shell
  git rm --cached TaskFlow.Api/tasks.db TaskFlow.Api/tasks.db-shm TaskFlow.Api/tasks.db-wal
  ```
  
  - Commit the change and add the `.gitignore` entry.

## Project structure and architecture

### Service registration pattern
TaskFlow.Api uses the **ServiceCollection Extension Pattern** for organizing dependency injection service registrations. This keeps `Program.cs` clean and maintainable by grouping related services into dedicated extension methods in the `Configuration` folder.

**Key extension classes:**
- `PersistenceServiceExtensions` — Database and repository services
- `ApplicationServiceExtensions` — Business logic services  
- `ValidationServiceExtensions` — FluentValidation configuration
- `HealthCheckServiceExtensions` — Health monitoring services
- `ApplicationInsightsServiceExtensions` — Application Insights telemetry
- `SwaggerServiceExtensions` — API documentation services
- `LoggingServiceExtensions` — Serilog configuration
- `JsonConfigurationExtensions` — JSON serialization options

📖 **For detailed guidance on adding new services or understanding the pattern, see [Service Registration Pattern Documentation](docs/SERVICE_REGISTRATION_PATTERN.md)**

### Key folders
- `Controllers/` — API controllers exposing REST endpoints
- `Services/` — Business logic layer
- `Repositories/` — Data access layer
- `Models/` — Domain entities
- `DTOs/` — Data transfer objects
- `Validators/` — FluentValidation validators
- `Configuration/` — Service registration extensions
- `Middleware/` — Custom middleware components
- `HealthChecks/` — Health check implementations
- `Migrations/` — EF Core database migrations

## Helpful files
- `Program.cs` — app startup, Serilog bootstrap, and DI registration via extension methods.
- `TaskDbContext.cs` — EF Core `DbContext`.
- `TaskItemsController.cs` — API controller (async EF Core usage, validation applied).
- `appsettings.json` / `appsettings.Development.json` — configuration and connection string.

## Development tips
- Add data annotations to DTOs (for example `[Required]` on `Title`) and enable automatic model validation if you want central validation behavior.
- Use `dotnet watch run` during development for faster iteration.
- Use the Swagger UI to exercise the endpoints and confirm behavior after schema or DTO changes.

## Contributing
- Follow standard Git workflows. Keep migrations descriptive and run them locally before pushing schema changes.
- Run tests and validate migrations with `dotnet ef database update` before opening a PR.
- When adding new services, follow the [Service Registration Pattern](docs/SERVICE_REGISTRATION_PATTERN.md) to keep `Program.cs` clean.
- Write unit and integration tests for new features.
- Follow the existing code style and conventions.

## License
No license specified. Add a `LICENSE` file if you plan to publish or share externally.