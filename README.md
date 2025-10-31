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
   - `dotne