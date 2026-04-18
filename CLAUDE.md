# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run
dotnet run --project TaskFlow.Api

# Test (all)
dotnet test

# Test (single)
dotnet test --filter "FullyQualifiedName~TaskServiceTests.GetAllTasksAsync_ShouldReturnAllTasks"

# Test with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Format check (CI enforces this)
dotnet format --verify-no-changes

# Docker (dev — includes Seq logging UI at http://localhost:5380)
docker compose up

# Docker (production)
docker compose -f docker-compose.prod.yml up
```

## Architecture

**Stack:** .NET 10, EF Core + SQLite, FluentValidation, OpenTelemetry → Seq, xUnit/Moq/FluentAssertions, Scalar/OpenAPI

**Layered flow:** `Controller → Service → Repository → EF Core (SQLite)`

**Extension method pattern** — `Program.cs` stays clean by delegating all service registration to methods in `Extensions/`. Each extension file owns one concern (persistence, versioning, health checks, OpenTelemetry, validation, JSON, OpenAPI). Adding a new cross-cutting concern means a new extension file, not touching `Program.cs`.

**API versioning** uses both URL path (`/api/v1/TaskItems`) and request header (`x-api-version`). Controllers live in `Controllers/V1/` and are decorated with `[ApiVersion("1.0")]`.

**Validation** is handled by FluentValidation validators in `Validators/` and applied globally via middleware registered in `ValidationServiceExtensions`. Controllers don't manually invoke validators.

**Health checks:** Three endpoints — `/health` (combined), `/health/ready` (DB connectivity, used as K8s readiness probe), `/health/live` (always up, liveness probe). Custom JSON writer in `HealthChecks/`.

**Migrations run on startup** when `Database:MigrateOnStartup` is `true` (default in Docker; disabled in production deploy if needed).

## Testing

Tests mirror the main project structure: `Controllers/V1/`, `Services/`, `Repositories/`, `Validators/`, `HealthChecks/`, `Extensions/`.

- Repository tests use `Microsoft.EntityFrameworkCore.InMemory` — no mocks for the DB layer.
- Service and controller tests use Moq to mock the layer below.
- CI enforces **75% line coverage** minimum (`ci.yml`).

## Key Configuration

`appsettings.Development.json` uses a local relative DB path (`./data/tasks.dev.db`). The container uses `/app/data/tasks.db`. The `OpenTelemetry` section configures the OTLP export endpoint and Seq API key — override via environment variables or `.env` file (gitignored).

## CI/CD

GitHub Actions workflows: `ci.yml` (lint → build → test → smoke test), `codeql.yml`, `container-scan.yml` (Trivy), `prod-deploy.yaml` / `qa-deploy.yaml` (Azure ACI via OIDC — no stored credentials).
