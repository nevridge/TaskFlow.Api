# TaskFlow.Api

Summary
- Small .NET 9 Web API demonstrating OpenAPI (Swagger) and a simple in-memory CRUD for `TaskItem`.
- Purpose: scaffold + experiment with API endpoints, Swagger UI, and local in-memory storage for development and testing.

What we changed / added today
- Enabled OpenAPI metadata and Swagger UI via `Swashbuckle` in `Program.cs`.
- Added a minimal API controller `TaskItemsController` with standard CRUD endpoints backed by an in-memory `List<TaskItem>`.
- Created the `TaskItem` model (`TaskFlow.Api\Models\TaskItem.cs`).
- Configured the app to show the Swagger UI at application root (Development environment) for quick exploration.

How to run
1. Ensure you have .NET 9 SDK installed.
2. From the solution/project root (where this README lives) run:
   - `dotnet restore`
   - `dotnet run --project TaskFlow.Api`
3. When running in the `Development` environment the Swagger UI is available at:
   - `https://localhost:{port}/` (or `http://localhost:{port}/`) — the exact port comes from `launchSettings.json` or the console output.

API endpoints
- GET `/api/TaskItems` — list all tasks
- GET `/api/TaskItems/{id}` — get a task by id
- POST `/api/TaskItems` — create a new task (returns `201 Created`)
- PUT `/api/TaskItems/{id}` — update an existing task (returns `204 No Content`)
- DELETE `/api/TaskItems/{id}` — delete a task (returns `204 No Content`)

Notes & recommendations
- Storage is purely in-memory and non-persistent — suitable for demos and tests only. Replace with a database or repository for production.
- Swagger UI is enabled only when `ASPNETCORE_ENVIRONMENT=Development` (recommended). Do not expose the dev UI in production unless secured.
- If you prefer the UI at `/swagger` instead of the app root, remove `c.RoutePrefix = string.Empty;` from `Program.cs` and update your launch settings (`launchSettings.json`) to `launchUrl: "swagger"`.

Add this README to the Solution Items folder
- In Visual Studio: open the solution, right-click the solution node in __Solution Explorer__ → __Add__ → __Existing Item...__, choose this `README.md`, then click the drop-down on the Add button and select __Add__ (it will appear under the Solution Items folder).
- Alternatively, keep the file at the repository root (it is already here) and add it to source control so teammates see it.

License
- No license specified. Add a license file if you intend to publish or share externally.