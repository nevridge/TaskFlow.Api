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
   ```bash
   docker-compose up
   ```
   The API will be available at `http://localhost:8080`. The development configuration automatically:
   - Runs in Development mode
   - Auto-applies database migrations
   - Persists the SQLite database in a Docker volume

2. **Stop the containers**:
   ```bash
   docker-compose down
   ```

3. **View logs**:
   ```bash
   docker-compose logs -f
   ```

#### Option 3: Docker CLI (Alternative)
Using Docker directly without compose:

```bash
cd TaskFlow.Api
docker build -f Dockerfile.dev -t taskflow-api:dev .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e Database__MigrateOnStartup=true taskflow-api:dev
```

### Production deployment

#### Building the Docker image
From the `TaskFlow.Api` directory (where the Dockerfile is located):
```bash
docker build -t taskflow-api:latest .
```

#### Running the container
```bash
# For persistent database storage, mount a host directory to /app (where tasks.db is stored):
docker run -d -p 8080:8080 -v $(pwd)/data:/app --name taskflow-api taskflow-api:latest
# Without the -v option, all data will be lost when the container is removed.
```

### Docker notes
- **Development**: Uses `Dockerfile.dev` with Debug configuration and SDK image for full debugging support
- **Production**: Uses `Dockerfile` with a 2-stage build:
  - **Stage 1**: Compiles the app with the .NET 9 SDK image
  - **Stage 2**: Runs it from the smaller ASP.NET 9 runtime image
- The `.dockerignore` file excludes build artifacts, dependencies, and unnecessary files from the build context
- The container exposes port 8080 by default
- **Persistence warning:** Without volume mounting, the SQLite database will be lost when the container is removed
- By default, the production Docker container runs in Production mode and migrations will **not** auto-apply. To enable automatic migrations, set either `ASPNETCORE_ENVIRONMENT=Development` or `Database__MigrateOnStartup=true` via environment variables when running the container.

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
- By default logs are written to console and to daily rolling files under `logs/`.
- Log configuration can be extended in `appsettings.json` (Serilog section) and via environment variables.
- Ensure `logs/` is ignored in Git (see .gitignore recommendations below).

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
  - `git rm --cached TaskFlow.Api/tasks.db TaskFlow.Api/tasks.db-shm TaskFlow.Api/tasks.db-wal`
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