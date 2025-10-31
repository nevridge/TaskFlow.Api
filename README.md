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