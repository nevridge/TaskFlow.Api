# TaskFlow.Api

A production-ready .NET 9 Web API for managing tasks, demonstrating modern development practices and cloud deployment patterns.

## Overview

TaskFlow.Api is a RESTful task management API built to showcase:
- **Clean architecture** with dependency injection and service patterns
- **Production deployment** with Docker and Azure integration
- **Automated CI/CD** with GitHub Actions, testing, and security scanning
- **Comprehensive monitoring** with health checks, logging, and telemetry

While functional as a task management system, this project serves as a portfolio piece demonstrating professional engineering practices and cloud-native development workflows.

## Key Features

- ✅ Full CRUD operations for task items via REST API
- 🗄️ Entity Framework Core with SQLite persistence
- 🔍 OpenAPI/Swagger documentation
- 📊 Structured logging with Serilog
- 🏥 Health check endpoints for container orchestration
- 🐳 Docker support for local and production deployment
- ☁️ Azure deployment automation with GitHub Actions
- 🔒 Security scanning (CodeQL + Trivy)
- 📈 Application Insights telemetry integration
- ✅ Automated testing with code coverage enforcement

## Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for containerized development)

### Run Locally (5 minutes)

1. **Clone and restore:**
   ```bash
   git clone https://github.com/nevridge/TaskFlow.Api.git
   cd TaskFlow.Api
   dotnet restore
   ```

2. **Run the application:**
   ```bash
   dotnet run --project TaskFlow.Api
   ```
   
3. **Access Swagger UI:**
   Navigate to `https://localhost:{port}` (port displayed in console output)

The application will automatically apply database migrations on first run.

### Run with Docker

```bash
docker compose up
```

Access the API at `http://localhost:8080`. Data persists in Docker volumes across container restarts.

## Documentation

### Primary Guides

- **[Getting Started](docs/GETTING_STARTED.md)** - Setup and run locally in 5 minutes
- **[API Reference](docs/API.md)** - Complete endpoint documentation with examples
- **[Architecture](docs/ARCHITECTURE.md)** - Design decisions, patterns, and quality practices
- **[Deployment](docs/DEPLOYMENT.md)** - Docker, Azure, and CI/CD workflows
- **[Contributing](docs/CONTRIBUTING.md)** - Development workflow and standards

### Reference Documentation

- **[Docker Configuration](docs/DOCKER_CONFIGURATION.md)** - Detailed dev vs prod Docker comparison
- **[Volumes](docs/VOLUMES.md)** - Volume configuration and persistence
- **[Health Checks](docs/HEALTH_CHECK_TESTING.md)** - Health check setup and testing
- **[Azure OIDC](docs/AZURE_OIDC_AUTHENTICATION.md)** - Azure authentication setup
- **[QA Deployment](docs/QA_DEPLOYMENT.md)** - Ephemeral QA environments
- **[Resource Naming](docs/DEPLOY.md)** - Azure naming standards
- **[Service Registration](docs/SERVICE_REGISTRATION_PATTERN.md)** - DI extension pattern
- **[Security Scanning](docs/SECURITY_SCANNING.md)** - CodeQL and Trivy
- **[Logging](docs/LOGGING.md)** - Serilog configuration
- **[Volume Testing](docs/VOLUME_TESTING.md)** - Testing volume persistence

## Project Structure

```
TaskFlow.Api/
├── Controllers/          # REST API endpoints
├── Services/             # Business logic layer
├── Repositories/         # Data access layer
├── Models/               # Domain entities
├── DTOs/                 # Data transfer objects
├── Validators/           # FluentValidation validators
├── Configuration/        # DI service registration extensions
├── HealthChecks/         # Custom health check implementations
├── Middleware/           # Custom middleware components
└── Migrations/           # EF Core database migrations
```

## Technology Stack

- **Framework:** .NET 9, ASP.NET Core
- **Database:** Entity Framework Core with SQLite
- **Logging:** Serilog (console + rolling file)
- **Validation:** FluentValidation
- **Testing:** xUnit, Moq, Coverlet (code coverage)
- **Monitoring:** Health checks, Application Insights
- **Documentation:** Swagger/OpenAPI
- **Deployment:** Docker, Azure Container Instances (ACI)
- **CI/CD:** GitHub Actions

## Portfolio Highlights

If you're evaluating this project for hiring or collaboration, here's what to notice:

### Architecture & Code Quality
- **Service registration pattern** keeps `Program.cs` clean and maintainable ([docs](docs/ARCHITECTURE.md#service-registration-pattern))
- **Repository pattern** abstracts data access
- **Dependency injection** throughout with proper lifetimes
- **Async/await** for all I/O operations
- **FluentValidation** for input validation

### DevOps & Deployment
- **Multi-stage Docker builds** for optimized production images
- **GitHub Actions workflows** for CI/CD with automated testing
- **Azure deployment automation** via OIDC (no stored credentials)
- **Infrastructure as Code** patterns in workflow definitions
- **Environment-specific configurations** (dev/production)

### Testing & Quality
- **Unit and integration tests** with 58%+ code coverage enforcement
- **Health checks** for readiness and liveness probes
- **Security scanning** with CodeQL (SAST) and Trivy (container scanning)
- **Automated code formatting** checks in CI

### Observability
- **Structured logging** with Serilog
- **Health check endpoints** (`/health`, `/health/ready`, `/health/live`)
- **Application Insights** telemetry integration
- **Detailed health check responses** with timing metrics

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/TaskItems` | List all tasks |
| GET | `/api/TaskItems/{id}` | Get task by ID |
| POST | `/api/TaskItems` | Create new task |
| PUT | `/api/TaskItems/{id}` | Update task |
| DELETE | `/api/TaskItems/{id}` | Delete task |
| GET | `/health` | Overall health status |
| GET | `/health/ready` | Readiness probe |
| GET | `/health/live` | Liveness probe |

See [API Reference](docs/API.md) for detailed endpoint documentation and examples.

## Configuration

Configure via `appsettings.json` or environment variables:

| Setting | Environment Variable | Default | Description |
|---------|---------------------|---------|-------------|
| Database connection | `ConnectionStrings__DefaultConnection` | `Data Source=tasks.db` | SQLite database path |
| Auto migrations | `Database__MigrateOnStartup` | `false` (true in Development) | Enable automatic migrations |
| Log path | `LOG_PATH` | `/app/logs/log.txt` | Serilog output path |
| App Insights | `ApplicationInsights__ConnectionString` | - | Telemetry connection string |

## Testing

```bash
# Run tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageThreshold=58
```

The CI pipeline enforces minimum 58% line coverage and fails builds below this threshold.

## Contributing

Contributions welcome! Please see [CONTRIBUTING.md](docs/CONTRIBUTING.md) for:
- Development workflow and branch strategy
- Code standards and conventions
- Testing requirements
- Pull request process

## License

No license specified. This project is primarily for portfolio and learning purposes.

## Contact

**Repository:** [nevridge/TaskFlow.Api](https://github.com/nevridge/TaskFlow.Api)

---

*Last updated: 2025-11-03*
