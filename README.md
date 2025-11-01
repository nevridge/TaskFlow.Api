# TaskFlow.Api

## Overview
TaskFlow.Api is a small .NET 9 Web API for managing task items (CRUD). The project is intended as a learning playground and a minimal foundation you can extend — it includes persistent storage via EF Core + SQLite, OpenAPI (Swagger), and structured logging with Serilog.

## Key features
- RESTful endpoints for task items (`GET`, `POST`, `PUT`, `DELETE`) exposed under `api/TaskItems`.
- Persistence using Entity Framework Core with SQLite (`TaskDbContext`).
- Migrations generated and tracked in `Migrations/` so schema changes are versioned.
- Structured logging with Serilog (console + rolling file sink).
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
   ```bash
   docker-compose up
   ```
   
   The API will be available at `http://localhost:8080`. The development configuration automatically:
   - Runs in Development mode
   - Auto-applies database migrations
   - Persists the SQLite database in a Docker volume

2. **Stop the containers**:
   
   **Bash/PowerShell:**
   ```bash
   docker-compose down
   ```

3. **View logs**:
   
   **Bash/PowerShell:**
   ```bash
   docker-compose logs -f
   ```

**Docker Compose Services:**
The `docker-compose.yml` defines the following service:
- `taskflow-api`: The main API service
  - **Image**: Built from `Dockerfile.dev`
  - **Ports**: Maps host port `8080` to container port `8080`
  - **Volumes**: 
    - `./data:/app/data` - Persists the SQLite database
    - `./logs:/app/logs` - Persists application logs
  - **Environment Variables**:
    - `ASPNETCORE_ENVIRONMENT=Development` - Runs in development mode
    - `ASPNETCORE_URLS=http://+:8080` - Configures Kestrel to listen on port 8080
    - `ASPNETCORE_HTTP_PORTS=8080` - Sets HTTP port
    - `Database__MigrateOnStartup=true` - Enables automatic database migrations
    - `DOTNET_RUNNING_IN_CONTAINER=true` - Indicates running in container

**Important Notes:**
- The `./data` and `./logs` directories will be created automatically on first run
- Database file is stored at `/app/data/tasks.dev.db` (configured in `appsettings.Development.json`) and persisted via the `./data` volume mount
- Application logs are written to `/app/logs/log.txt` and persisted via the `./logs` volume mount
- Ensure these directories are excluded from version control (they're in `.gitignore`)
- For detailed volume configuration, see [docs/volumes.md](docs/volumes.md)

#### Option 3: Docker CLI (Alternative)
Using Docker directly without compose:

**Bash/PowerShell:**
```bash
cd TaskFlow.Api
docker build -f Dockerfile.dev -t taskflow-api:dev .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e Database__MigrateOnStartup=true taskflow-api:dev
```

### Production deployment

#### Building the Docker image
From the repository root (where `docker-compose.yml` is located):

**Bash/PowerShell:**
```bash
docker build -f TaskFlow.Api/Dockerfile -t taskflow-api:latest .
```

Note: The production `Dockerfile` uses a multi-stage build with the repository root as context.

#### Running the container

**Bash:**
```bash
# For persistent database and log storage, mount host directories
docker run -d -p 8080:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  --name taskflow-api taskflow-api:latest
# Without the -v options, all data and logs will be lost when the container is removed.
```

**PowerShell:**
```powershell
# For persistent database and log storage, mount host directories
docker run -d -p 8080:8080 `
  -v ${PWD}/data:/app/data `
  -v ${PWD}/logs:/app/logs `
  --name taskflow-api taskflow-api:latest
# Without the -v options, all data and logs will be lost when the container is removed.
```

**Important**: The application uses `/app/data/tasks.db` for the database and `/app/logs/log.txt` for logs by default. To use custom paths, override via environment variables:

**Bash:**
```bash
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/production.db" \
  -e LOG_PATH=/app/logs/taskflow.log \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  --name taskflow-api taskflow-api:latest
```

**PowerShell:**
```powershell
docker run -d -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/production.db" `
  -e LOG_PATH=/app/logs/taskflow.log `
  -v ${PWD}/data:/app/data `
  -v ${PWD}/logs:/app/logs `
  --name taskflow-api taskflow-api:latest
```

### Docker configuration summary
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

- **docker-compose.yml**:
  - Builds using `Dockerfile.dev` from the `TaskFlow.Api` directory
  - Mounts `./data:/app/data` for database persistence
  - Mounts `./logs:/app/logs` for log persistence
  - Configures development environment with automatic migrations

- **Key environment variables**:
  - `ASPNETCORE_ENVIRONMENT`: Controls environment (Development/Production)
  - `ASPNETCORE_URLS` / `ASPNETCORE_HTTP_PORTS`: Configure Kestrel ports
  - `Database__MigrateOnStartup`: Enable/disable automatic migrations (true/false)
  - `ConnectionStrings__DefaultConnection`: Override SQLite database path (default: `/app/data/tasks.db`)
  - `LOG_PATH`: Override log file path (default: `/app/logs/log.txt`)
  - `DOTNET_RUNNING_IN_CONTAINER`: Signals container runtime

For detailed volume configuration and troubleshooting, see [docs/volumes.md](docs/volumes.md).

### Docker notes
- The `.dockerignore` file excludes build artifacts, dependencies, and unnecessary files from the build context
- The container exposes port 8080 by default
- **Persistence warning:** Without volume mounting, the SQLite database and logs will be lost when the container is removed
- By default, the production Docker container runs in Production mode and migrations will **not** auto-apply. To enable automatic migrations, set either `ASPNETCORE_ENVIRONMENT=Development` or `Database__MigrateOnStartup=true` via environment variables when running the container.

## Azure deployment

The project includes automated deployment workflows for Azure App Service using GitHub Actions. Two deployment options are available:

### Production deployment to Azure App Service

The production deployment workflow (`.github/workflows/deploy.yaml`) automatically deploys to Azure when you push a tag or manually trigger the workflow.

#### Prerequisites
1. **Azure subscription** with permissions to create resources
2. **Azure service principal** with Contributor access to your subscription
3. **GitHub repository secret** named `AZURE_CREDENTIALS` containing service principal JSON

#### Creating the Azure service principal

**Note**: The workflows currently use the legacy `--sdk-auth` format. While this format still works, it's deprecated. Here's the command:

**Bash:**
```bash
az ad sp create-for-rbac \
  --name "TaskFlowDeployment" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

**PowerShell:**
```powershell
az ad sp create-for-rbac `
  --name "TaskFlowDeployment" `
  --role contributor `
  --scopes /subscriptions/{subscription-id} `
  --sdk-auth
```

Copy the output JSON and add it as a repository secret named `AZURE_CREDENTIALS` in GitHub:
- Go to **Settings → Secrets and variables → Actions → New repository secret**
- Name: `AZURE_CREDENTIALS`
- Value: Paste the entire JSON output from the command above

**Modern alternative**: For new deployments, consider migrating to OpenID Connect (OIDC) authentication which doesn't require storing credentials:

**Bash:**
```bash
# Create the service principal (without --sdk-auth)
az ad sp create-for-rbac \
  --name "TaskFlowDeployment" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}

# Then configure federated credentials for GitHub Actions
# See: https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure
```

**PowerShell:**
```powershell
# Create the service principal (without --sdk-auth)
az ad sp create-for-rbac `
  --name "TaskFlowDeployment" `
  --role contributor `
  --scopes /subscriptions/{subscription-id}

# Then configure federated credentials for GitHub Actions
# See: https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure
```

#### Deployment workflow configuration

The workflow is configured with the following Azure resources (in `.github/workflows/deploy.yaml`):
- **Resource Group**: `TaskFlowRG` (location: `eastus`)
- **Azure Container Registry (ACR)**: `taskflowregistry`
- **App Service Plan**: `TaskFlowAppServicePlan` (Linux, B1 SKU)
- **Web App**: `taskflowapi2074394909`
- **ACR Image**: `taskflowapi9`

**Note**: You should customize these resource names in the workflow file to match your requirements.

#### Triggering a deployment

**Option 1: Tag-based deployment (recommended)**

**Bash/PowerShell:**
```bash
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

The ephemeral deployment workflow (`.github/workflows/ephemeral-deploy.yaml`) allows you to create temporary test environments using Azure Container Instances.

#### Use cases
- Testing pull requests in an isolated environment
- Short-lived demo environments
- Integration testing

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
2. Creates Azure Container Instance
3. Exposes API on public FQDN with DNS label
4. Performs smoke test on `/health` endpoint

The deployment creates a unique ACI instance with DNS: `taskflow-aci-{run-id}.{region}.azurecontainer.io`

#### Teardown
To delete the ephemeral environment:
1. Run the workflow with `action: teardown`
2. Provide the `resource_group` name from the deployment
3. The workflow will delete the entire resource group

**Note**: Ephemeral deployments use the same ACR as production by default. You can specify a different ACR name if needed.

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
2. **Verify Azure credentials**: Ensure `AZURE_CREDENTIALS` secret is valid and has proper permissions
3. **Check resource quotas**: Ensure your subscription has available quota
4. **Health check failures**: Check App Service logs in Azure Portal
5. **Container logs**: View logs via Azure CLI:
   
   **Bash/PowerShell:**
   ```shell
   az webapp log tail --name {WEBAPP_NAME} --resource-group {RESOURCE_GROUP}
   ```

## CI/CD workflows

The project includes two GitHub Actions workflows for continuous deployment:

### Workflow: Deploy to Azure Production
**File**: `.github/workflows/deploy.yaml`

**Triggers**:
- Manual trigger via workflow_dispatch
- Automatic trigger on tags matching `v*` (e.g., `v1.0.0`, `v2.1.3`)

**Required secrets**:
- `AZURE_CREDENTIALS`: Service principal JSON with subscription access

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
- `AZURE_CREDENTIALS`: Service principal JSON with subscription access

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
- **Purpose**: Returns the overall health status of the application
- **Checks**: Validates database connectivity via EF Core DbContext
- **Response**: HTTP 200 (Healthy) or HTTP 503 (Unhealthy)
- **Response format**: JSON with detailed status
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
  "results": {
    "TaskDbContext": {
      "status": "Healthy"
    }
  }
}
```

**Unhealthy response** (HTTP 503):
```json
{
  "status": "Unhealthy",
  "results": {
    "TaskDbContext": {
      "status": "Unhealthy",
      "description": "Database connection failed"
    }
  }
}
```

#### 2. Readiness check: `/health/ready`
- **Purpose**: Indicates if the application is ready to receive traffic
- **Checks**: Database connectivity and any checks tagged with "ready"
- **Response**: HTTP 200 (Ready) or HTTP 503 (Not ready)
- **Use case**: Kubernetes readiness probes, load balancer registration

**Note**: Currently configured but filtering for "ready" tag. To add readiness-specific checks, tag them during registration:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>(tags: new[] { "ready" });
```

#### 3. Liveness check: `/health/live`
- **Purpose**: Indicates if the application is running and not deadlocked
- **Checks**: Only checks tagged with "live" (currently none - returns healthy by default)
- **Response**: HTTP 200 (Alive) or HTTP 503 (Dead)
- **Use case**: Kubernetes liveness probes, restart decisions

**Note**: Currently returns healthy as no checks are tagged with "live". Liveness checks should be lightweight and not depend on external services.

### Health check implementation

The health checks are configured in `Program.cs`:

```csharp
// Register health checks with database connectivity validation
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>();

// Map health check endpoints
app.MapHealthChecks("/health");           // Overall health
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("live")
});
```

### Configuring health checks for container orchestrators

#### Docker Compose health check
Add to `docker-compose.yml`:

**Note**: The health check runs inside the container. The example below uses `curl`, which should be installed in your container image. Our Dockerfiles use ASP.NET base images that include `curl`. If using a minimal base image, you may need to install `curl` or use an alternative health check method.

```yaml
services:
  taskflow-api:
    # ... other configuration
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

#### Kubernetes probes
Add to your Kubernetes deployment manifest:
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
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 20
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
```

#### Azure App Service health check
Configure in Azure Portal or via ARM template:
```json
{
  "properties": {
    "siteConfig": {
      "healthCheckPath": "/health"
    }
  }
}
```

Or via Azure CLI:

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

### Health check best practices

1. **Separate concerns**: Use `/health/live` for process health, `/health/ready` for dependencies
2. **Fail fast**: Health checks should complete quickly (< 5 seconds)
3. **Database checks**: Only include database in readiness, not liveness
4. **Startup time**: Configure appropriate `initialDelaySeconds` to allow for migrations
5. **Monitoring**: Set up alerts on repeated health check failures
6. **Logging**: Health check failures are logged by Serilog for troubleshooting

### Extending health checks

To add custom health checks:

```csharp
// Example: Add custom checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>(tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddUrlGroup(new Uri("https://api.external.com/status"), 
                 name: "external-api", 
                 tags: new[] { "ready" });
```

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
- Ensure `logs/` is ignored in Git (see .gitignore recommendations below).
- For Docker volume configuration, see [docs/volumes.md](docs/volumes.md).

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

## Helpful files
- `Program.cs` — app startup, Serilog bootstrap, DI registrations, and conditional migration logic.
- `TaskDbContext.cs` — EF Core `DbContext`.
- `TaskItemsController.cs` — API controller (async EF Core usage, validation applied).
- `appsettings.json` / `appsettings.Development.json` — configuration and connection string.

## Development tips
- Add data annotations to DTOs (for example `[Required]` on `Title`) and enable automatic model validation if you want central validation behavior.
- Use `dotnet watch run` during development for faster iteration.
- Use the Swagger UI to exercise the endpoints and confirm behavior after schema or DTO changes.

## Contributing
- Follow standard Git workflows. Keep migrations descriptive and run them locally before pushing schema changes.
- Run tests (if added) and validate migrations with `dotnet ef database update` before opening a PR.

## License
No license specified. Add a `LICENSE` file if you plan to publish or share externally.