# Architecture & Design

This document explains the architectural decisions, design patterns, and quality practices used in TaskFlow.Api.

## Table of Contents

- [Overview](#overview)
- [Architecture Principles](#architecture-principles)
- [Application Structure](#application-structure)
- [Design Patterns](#design-patterns)
- [Service Registration Pattern](#service-registration-pattern)
- [Health Checks](#health-checks)
- [Data Persistence](#data-persistence)
- [Logging Strategy](#logging-strategy)
- [Quality Practices](#quality-practices)

## Overview

TaskFlow.Api follows a **layered architecture** pattern, separating concerns into distinct layers:

```
┌─────────────────────────────────────┐
│         Controllers (API)           │  ← REST endpoints
├─────────────────────────────────────┤
│      Services (Business Logic)      │  ← Domain logic
├─────────────────────────────────────┤
│    Repositories (Data Access)       │  ← Data operations
├─────────────────────────────────────┤
│     EF Core DbContext (ORM)         │  ← Database mapping
├─────────────────────────────────────┤
│        SQLite Database              │  ← Data storage
└─────────────────────────────────────┘
```

**Benefits:**
- Clear separation of concerns
- Testable components at each layer
- Easy to modify or replace individual layers
- Follows SOLID principles

## Architecture Principles

### 1. Dependency Injection (DI)

All services are registered in the DI container and injected via constructors. This enables:
- Loose coupling between components
- Easy unit testing with mock implementations
- Centralized service lifetime management

### 2. Async/Await Throughout

All I/O operations (database, logging, HTTP) use async/await:
- Better scalability under load
- Non-blocking operations
- Consistent pattern across the codebase

### 3. Repository Pattern

Data access is abstracted through repository interfaces:
- Decouples business logic from data access implementation
- Enables testing without a real database
- Centralizes data access logic

### 4. Configuration-Based Behavior

Environment-specific behavior controlled via configuration:
- No code changes needed for different environments
- Easy to override with environment variables
- Supports local, Docker, and cloud deployments

## Application Structure

```
TaskFlow.Api/
├── Extensions/                 # DI service registration extensions
│   ├── PersistenceServiceExtensions.cs
│   ├── ApplicationServiceExtensions.cs
│   ├── ValidationServiceExtensions.cs
│   ├── HealthCheckServiceExtensions.cs
│   ├── ApiVersioningServiceExtensions.cs
│   ├── OpenApiServiceExtensions.cs
│   ├── OpenTelemetryServiceExtensions.cs
│   └── JsonConfigurationExtensions.cs
├── Controllers/                # REST API endpoints
│   └── V1/
│       ├── TaskItemsController.cs
│       └── StatusController.cs
├── Data/                       # EF Core DbContext
│   └── TaskDbContext.cs
├── DTOs/                       # Data transfer objects
│   ├── CreateTaskItemDto.cs
│   ├── UpdateTaskItemDto.cs
│   └── TaskItemResponseDto.cs
├── HealthChecks/               # Health check implementations
│   └── HealthCheckResponseWriter.cs
├── Migrations/                 # EF Core migrations
├── Models/                     # Domain entities
│   ├── TaskItem.cs
│   └── Status.cs
├── Providers/                  # Shared providers
│   └── JsonSerializerOptionsProvider.cs
├── Repositories/               # Data access layer
│   ├── ITaskRepository.cs
│   ├── TaskRepository.cs
│   ├── IStatusRepository.cs
│   └── StatusRepository.cs
├── Services/                   # Business logic
│   ├── ITaskService.cs
│   ├── TaskService.cs
│   ├── IStatusService.cs
│   └── StatusService.cs
├── Validators/                 # FluentValidation validators
│   ├── TaskItemValidator.cs
│   └── StatusValidator.cs
└── Program.cs                  # Application entry point
```

### Layer Responsibilities

**Controllers (API Layer)**
- Handle HTTP requests/responses
- Validate input using DTOs
- Call service layer methods
- Return appropriate HTTP status codes
- Do NOT contain business logic

**Services (Business Logic Layer)**
- Implement business rules
- Coordinate operations across repositories
- Handle exceptions and error cases
- Transform between DTOs and domain models
- Do NOT access database directly

**Repositories (Data Access Layer)**
- Perform CRUD operations
- Execute queries
- Handle EF Core specifics
- Abstract database implementation details
- Do NOT contain business logic

## Design Patterns

### Service Registration Pattern

TaskFlow.Api uses the **ServiceCollection Extension Pattern** to organize dependency injection registrations. This keeps `Program.cs` clean and maintainable.

#### Extension Classes

| Extension Class | Purpose | Services Registered |
|----------------|---------|---------------------|
| `PersistenceServiceExtensions` | Database and data access | `TaskDbContext`, `ITaskRepository`, `IStatusRepository` |
| `ApplicationServiceExtensions` | Business logic services | `ITaskService`, `IStatusService` |
| `ValidationServiceExtensions` | Input validation | FluentValidation validators |
| `HealthCheckServiceExtensions` | Health monitoring | Database and self health checks |
| `ApiVersioningServiceExtensions` | API versioning | API versioning middleware and explorer |
| `OpenApiServiceExtensions` | API documentation | OpenAPI/Scalar services |
| `OpenTelemetryServiceExtensions` | Observability | OpenTelemetry tracing, metrics, and logging |
| `JsonConfigurationExtensions` | JSON serialization | JSON serializer options |

#### Usage in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Clean, readable service registration
builder.Logging.AddApplicationLogging(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddApiVersioningConfiguration();
OpenApiServiceExtensions.AddOpenApi(builder.Services);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddValidation();
builder.Services.AddApplicationHealthChecks();
builder.Services.AddOpenTelemetryObservability(builder.Configuration);
builder.Services.ConfigureJsonSerialization();
```

#### Creating New Extensions

When adding new services, follow this pattern:

1. **Create extension class** in `Extensions/` folder
2. **Define static extension method** on `IServiceCollection`
3. **Group related services** logically
4. **Update Program.cs** with single method call

**Example:**

```csharp
namespace TaskFlow.Api.Extensions;

public static class CachingServiceExtensions
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        
        // Configure options
        services.Configure<CacheOptions>(
            configuration.GetSection("Caching"));
        
        // Register cache service
        services.AddScoped<ICacheService, CacheService>();
        
        return services;
    }
}
```

**Benefits:**
- `Program.cs` stays concise and readable
- Related services grouped together
- Easy to find where services are registered
- Testable in isolation
- Reusable across projects

For detailed guidance, see the [Service Registration Pattern Documentation](SERVICE_REGISTRATION_PATTERN.md).

### Repository Pattern

**Interface:**
```csharp
public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<TaskItem> AddAsync(TaskItem taskItem);
    Task UpdateAsync(TaskItem taskItem);
    Task DeleteAsync(int id);
}
```

**Benefits:**
- Testable with mock implementations
- Can swap database implementations
- Centralizes data access logic
- Clear contract for data operations

### DTO Pattern

Data Transfer Objects separate API contracts from domain models:

**Domain Model (internal):**
```csharp
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

**Create DTO (API input):**
```csharp
public class CreateTaskItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? StatusId { get; set; }
    public bool IsComplete { get; set; }
}
```

**Response DTO (API output):**
```csharp
public class TaskItemResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public string? StatusName { get; set; }
}
```

**Benefits:**
- API contract decoupled from domain model
- Internal model can evolve independently
- Control exactly what's exposed in API
- Easier to add validation attributes

### Validation with FluentValidation

Input validation is declarative and testable:

```csharp
public class TaskItemCreateDtoValidator : AbstractValidator<TaskItemCreateDto>
{
    public TaskItemCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 1000 characters");
    }
}
```

**Benefits:**
- Validation rules in one place
- Unit testable
- Clear, readable rules
- Consistent error messages

## Health Checks

TaskFlow.Api implements comprehensive health checks for container orchestration and monitoring.

### Health Check Endpoints

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `/health` | Overall health | Database + Self |
| `/health/ready` | Readiness probe | Database connectivity |
| `/health/live` | Liveness probe | Application responsiveness |

### Implementation

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>(
        name: "database",
        tags: new[] { "ready" })
    .AddCheck(
        name: "self",
        check: () => HealthCheckResult.Healthy("Application is running"),
        tags: new[] { "live" });
```

### Tag Strategy

- **`ready` tag:** Checks that must pass before accepting traffic (database, external dependencies)
- **`live` tag:** Lightweight checks confirming the process is responsive

**Rationale:**
- Liveness checks avoid database to prevent restart loops from transient DB issues
- Readiness checks include database to prevent routing traffic before the app is ready
- Separate tags enable appropriate probe configuration in Kubernetes/Docker

### Custom Response Format

Health checks return detailed JSON responses:

```json
{
  "status": "Healthy",
  "totalDuration": 25.4551,
  "results": [
    {
      "name": "database",
      "status": "Healthy",
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

**Benefits:**
- Detailed troubleshooting information
- Duration metrics for performance monitoring
- Standard HTTP status codes (200 = healthy, 503 = unhealthy)

### Orchestration Configuration

**Docker Compose:**
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s  # Grace period for migrations
```

**Kubernetes:**
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 60
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 45
  periodSeconds: 5
```

**Key considerations:**
- `start_period`/`initialDelaySeconds` allows time for database migrations
- Liveness uses lightweight `/health/live` (no DB dependency)
- Readiness uses `/health/ready` (includes DB check)

## Data Persistence

### Entity Framework Core

**DbContext:**
```csharp
public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Status> Statuses => Set<Status>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        });
    }
}
```

### Migration Strategy

**Development:**
- Automatic migrations via `Database.MigrateOnStartup = true`
- Fast iteration during development

**Production:**
- Manual migration control (automatic disabled by default)
- Run migrations during deployment via CI/CD
- Avoids concurrent migration issues

**Migration commands:**
```bash
# Create new migration
dotnet ef migrations add MigrationName --project TaskFlow.Api

# Apply migrations
dotnet ef database update --project TaskFlow.Api

# Revert migration
dotnet ef database update PreviousMigrationName --project TaskFlow.Api
```

### SQLite Usage

**Why SQLite:**
- Zero configuration required
- Single-file database
- Perfect for demos and learning
- Easy Docker volume persistence
- No external database server needed

**Limitations:**
- Not for high-concurrency production workloads
- Limited to single server (no shared database)
- Good for: demos, learning, prototypes, low-traffic apps

**Production alternative:**
Swap connection string to use SQL Server, PostgreSQL, MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql.example.com;Database=TaskFlow;..."
  }
}
```

No code changes needed—EF Core handles the abstraction.

## Logging Strategy

### OpenTelemetry Configuration

Structured logging with OpenTelemetry exports logs via OTLP.

**Application logging setup:**
```csharp
builder.Logging.AddApplicationLogging(builder.Configuration, builder.Environment);
```

This call:
- Clears all default logging providers
- Adds an OTLP exporter targeting the configured endpoint
- Adds a console exporter in Development only

### OTLP Exporter Settings

| Setting | Environment Variable | Default |
|---------|---------------------|---------|
| Service name | `OpenTelemetry__ServiceName` | `TaskFlow.Api` |
| OTLP endpoint | `OpenTelemetry__Endpoint` | `http://localhost:5341/ingest/otlp` |
| Auth header | `OpenTelemetry__Header` | *(none)* |
| Protocol | `OpenTelemetry__Protocol` | `http/protobuf` |

### Health Check Logging

Health check failures are automatically logged:
- Error level for unhealthy checks
- Warning level for degraded checks
- Includes exception details
- Timestamped for correlation

For detailed logging documentation, see [Logging Guide](LOGGING.md).

## Quality Practices

### 1. Automated Testing

**Test structure:**
```
TaskFlow.Api.Tests/
├── Controllers/        # Controller unit tests
├── Services/           # Service layer tests
├── Repositories/       # Repository tests with in-memory DB
├── Validators/         # Validation logic tests
└── Extensions/         # DI registration integration tests
```

**Code coverage enforcement:**
- Minimum 58% line coverage required
- CI pipeline fails if coverage drops
- Coverage reports generated in CI

**Run tests:**
```bash
dotnet test
dotnet test /p:CollectCoverage=true
```

### 2. Security Scanning

**CodeQL (SAST):**
- Static analysis of C# code
- Identifies security vulnerabilities
- Runs on push to main, PRs, and weekly schedule
- Results in GitHub Security tab

**Trivy (Container Scanning):**
- Scans Docker images for vulnerabilities
- Checks OS packages and .NET libraries
- Fails on CRITICAL/HIGH severity findings
- Runs on push to main, PRs, and weekly schedule

See [Security Scanning Documentation](SECURITY_SCANNING.md) for details.

### 3. Code Formatting

**Automated formatting checks:**
- `dotnet format` enforces consistent style
- CI verifies formatting on every push
- Run locally: `dotnet format`

### 4. Dependency Injection Best Practices

**Service lifetimes:**
- **Scoped:** DbContext, repositories, services (default)
- **Transient:** Validators, lightweight services
- **Singleton:** Configuration, logging, caching

**Registration pattern:**
- Interface-based dependencies
- Constructor injection only
- No service locator pattern
- Clear, explicit registrations

### 5. Error Handling

**API responses:**
- 200 OK: Successful operation
- 201 Created: Resource created
- 204 No Content: Successful delete
- 400 Bad Request: Validation failure
- 404 Not Found: Resource doesn't exist
- 500 Internal Server Error: Unexpected errors

**Validation errors:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required"]
  }
}
```

### 6. API Versioning

**Implemented using Microsoft's Asp.Versioning packages** following official best practices:

**Configuration:**
```csharp
builder.Services.AddApiVersioningConfiguration();
```

**Features:**
- URL path versioning (primary): `/api/v1/TaskItems`, `/api/v2/TaskItems`
- Header versioning (fallback): `x-api-version: 1.0`
- Default version: 1.0
- Version discovery via response headers: `api-supported-versions`
- Multi-version OpenAPI/Scalar documentation

**Controller implementation:**
```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TaskItemsController : ControllerBase
{
    // Version 1.0 endpoints
}
```

**Benefits:**
- Backward compatibility for existing clients
- Gradual migration path for API consumers
- Multiple versions coexist safely
- Clear version visibility in URLs

See [API Versioning Guide](API_VERSIONING.md) for complete documentation.

## Documentation Philosophy

Documentation in TaskFlow.Api follows an audience-focused approach:

**For Developers:**
- Quick start in <5 minutes
- Clear setup instructions
- Examples and code snippets
- Troubleshooting guidance

**For Employers/Reviewers:**
- Visible quality indicators
- Architecture decisions explained
- CI/CD and deployment patterns
- Testing and security practices

**Structure:**
- **README:** Overview, quick start, key highlights
- **docs/:** Comprehensive reference material
- **Code comments:** Only where necessary to explain "why"

This approach ensures:
- Easy onboarding for new developers
- Clear skill assessment for hiring managers
- Maintainable documentation that stays current

---

[← Back to README](../README.md) | [Next: Deployment Guide →](DEPLOYMENT.md)
