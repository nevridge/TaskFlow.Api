# Getting Started with TaskFlow.Api

This guide walks you through setting up TaskFlow.Api for local development.

## Prerequisites

### Required
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed
- A terminal/command prompt
- A code editor (Visual Studio, VS Code, Rider, etc.)

### Optional
- [Docker Desktop](https://www.docker.com/products/docker-desktop) - for containerized development
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.0+) - for IDE-based Docker support
- [Postman](https://www.postman.com/) - for API testing

## Local Development Setup

### Option 1: Run Directly with .NET CLI (Fastest)

1. **Clone the repository:**
   ```bash
   git clone https://github.com/nevridge/TaskFlow.Api.git
   cd TaskFlow.Api
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run --project TaskFlow.Api
   ```

4. **Access the API:**
   - The console will display the URL (e.g., `https://localhost:5001`)
   - Navigate to the URL in your browser to see Swagger UI
   - API endpoints are available at `https://localhost:{port}/api/TaskItems`

The application will:
- Automatically apply database migrations on first run
- Create a SQLite database at `TaskFlow.Api/tasks.db`
- Write logs to `TaskFlow.Api/logs/log.txt`

### Option 2: Docker Compose (Recommended for Production-Like Testing)

1. **Start the application:**
   ```bash
   docker compose up
   ```

2. **Access the API:**
   - API: `http://localhost:8080`
   - Health check: `http://localhost:8080/health`

3. **View logs:**
   ```bash
   docker compose logs -f
   ```

4. **Stop the application:**
   ```bash
   docker compose down
   ```
   
   Note: This preserves data in Docker volumes. To remove data as well:
   ```bash
   docker compose down -v
   ```

**What's included:**
- Development environment configuration
- Automatic database migrations
- Persistent storage via Docker named volumes
- Log persistence

### Option 3: Visual Studio (Windows/Mac)

1. **Open the solution:**
   - Launch Visual Studio 2022
   - Open `TaskFlow.Api.sln`

2. **Run with Docker (if Container Development Tools installed):**
   - Select **"Container (Dockerfile.dev)"** from debug dropdown
   - Press **F5** or click Run
   - Visual Studio will handle Docker setup automatically

3. **Run without Docker:**
   - Select **"TaskFlow.Api"** from debug dropdown
   - Press **F5** or click Run

## Verify Installation

### Test the Health Endpoint

```bash
curl http://localhost:8080/health
```

Expected response:
```json
{
  "status": "Healthy",
  "totalDuration": 25.4551,
  "results": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "",
      "duration": 20.355
    },
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running",
      "duration": 1.0933
    }
  ]
}
```

### Create Your First Task

**Using curl:**
```bash
curl -X POST http://localhost:8080/api/TaskItems \
  -H "Content-Type: application/json" \
  -d '{"title":"My First Task","description":"Getting started with TaskFlow","isComplete":false}'
```

**Using PowerShell:**
```powershell
Invoke-RestMethod -Uri http://localhost:8080/api/TaskItems `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"title":"My First Task","description":"Getting started with TaskFlow","isComplete":false}'
```

### List All Tasks

```bash
curl http://localhost:8080/api/TaskItems
```

## Using Swagger UI

In Development mode, Swagger UI is automatically enabled:

1. Navigate to `https://localhost:{port}` (for direct run) or `http://localhost:8080` (for Docker)
2. You'll see an interactive API documentation interface
3. Click "Try it out" on any endpoint to test it
4. No authentication is required for local development

## Using Postman

A Postman collection is available for testing:

1. Import the collection:
   - Collection URL: https://studyplan-9664.postman.co/workspace/StudyPlan~b854a959-3425-41a8-9125-d9e7335da054/collection/102031-e46c6909-f827-46a6-affb-06cae2c01a09
   - In Postman: **File > Import** → paste URL

2. Set the environment:
   - Variable name: `baseUrl`
   - Value: `http://localhost:8080` (or your local URL)

3. Run requests from the collection

## Database Migrations

### Automatic Migrations (Development)

In Development mode, migrations are automatically applied on startup. No action needed!

### Manual Migrations

To run migrations manually:

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project TaskFlow.Api
```

### Create New Migrations

When you modify entity models:

```bash
dotnet ef migrations add YourMigrationName --project TaskFlow.Api
```

## Configuration

### Connection String

By default, the application uses SQLite with a file-based database:

**Development:** `tasks.db` in the project directory  
**Docker:** `/app/data/tasks.dev.db` (persisted in Docker volume)

To change the database location, set the environment variable:
```bash
export ConnectionStrings__DefaultConnection="Data Source=/custom/path/database.db"
```

### Logging

Logs are written to:
- **Console:** Always enabled
- **File:** `logs/log.txt` (or `/app/logs/log.txt` in Docker)

To change log location:
```bash
export LOG_PATH="/custom/path/application.log"
```

### Auto-Migration Control

To disable automatic migrations in Development:

**appsettings.Development.json:**
```json
{
  "Database": {
    "MigrateOnStartup": false
  }
}
```

Or via environment variable:
```bash
export Database__MigrateOnStartup=false
```

## Development Workflow

### Watch Mode (Hot Reload)

For faster development iteration:

```bash
dotnet watch run --project TaskFlow.Api
```

The application will automatically reload when you save code changes.

### Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Watch mode for tests
dotnet watch test --project TaskFlow.Api.Tests
```

### Code Formatting

```bash
# Check formatting
dotnet format --verify-no-changes

# Apply formatting
dotnet format
```

## Troubleshooting

### Port Already in Use

If the default port is already in use, specify a different port:

```bash
dotnet run --project TaskFlow.Api --urls "https://localhost:5051"
```

### Database Locked

If you see "database is locked" errors:
- Close any other applications accessing the database
- Delete the database file and restart (loses data)
- Use Docker instead for better isolation

### Migrations Fail

If migrations fail to apply:
1. Check database file permissions
2. Delete the database file and retry
3. Run migrations manually: `dotnet ef database update --project TaskFlow.Api`

### Docker Issues

**Container won't start:**
- Ensure Docker Desktop is running
- Check for port conflicts: `docker ps`
- View logs: `docker compose logs`

**Data not persisting:**
- Volumes are preserved by default with `docker compose down`
- To remove volumes: `docker compose down -v`

## Next Steps

- **Explore the API:** Try all CRUD operations in Swagger UI
- **Review the code:** Check out the architecture in [ARCHITECTURE.md](ARCHITECTURE.md)
- **Deploy to Azure:** See [DEPLOYMENT.md](DEPLOYMENT.md) for deployment instructions
- **Run tests:** `dotnet test` to see the test suite
- **Add features:** See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines

## Additional Resources

- [Architecture Documentation](ARCHITECTURE.md)
- [Deployment Guide](DEPLOYMENT.md)
- [API Reference](API.md)
- [Contributing Guidelines](CONTRIBUTING.md)

---

[← Back to README](../README.md) | [Next: Architecture →](ARCHITECTURE.md)
