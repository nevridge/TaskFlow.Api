# TaskFlow.Api

## Overview
TaskFlow.Api is a simple .NET 9 Web API project designed to manage task items through basic CRUD operations. This project serves as a learning and career development exercise, focusing on building and experimenting with modern .NET Web APIs, OpenAPI documentation, and in-memory data storage. It represents the initial steps in a broader plan to deepen skills in API development, best practices, and potentially expanding to full-stack applications or cloud deployments.

## Features
- RESTful API endpoints for creating, reading, updating, and deleting task items.
- OpenAPI (Swagger) integration for interactive API documentation and testing.
- In-memory data storage using a `List<TaskItem>` for simplicity and quick prototyping.
- Configured for development with Swagger UI accessible during local runs.

## Getting Started
### Prerequisites
- .NET 9 SDK installed.

### Running the Application
1. Clone or navigate to the project directory.
2. Restore dependencies: `dotnet restore`.
3. Run the project: `dotnet run --project TaskFlow.Api`.
4. In Development mode, access the Swagger UI at `https://localhost:{port}/` (or `http://localhost:{port}/`), where the port is specified in `launchSettings.json` or console output.

## API Endpoints
- `GET /api/TaskItems` — Retrieve all task items.
- `GET /api/TaskItems/{id}` — Retrieve a specific task item by ID.
- `POST /api/TaskItems` — Create a new task item.
- `PUT /api/TaskItems/{id}` — Update an existing task item.
- `DELETE /api/TaskItems/{id}` — Delete a task item by ID.

## Notes
- Data is stored in-memory and is not persistent across application restarts. This is ideal for development and testing; consider integrating a database for production use.
- Swagger UI is enabled only in the Development environment for security. Avoid exposing it in production unless properly secured.
- This project is in its early stages and may evolve with additional features, such as authentication, database integration, or frontend components.

## License
No license specified. Add a license file if you intend to publish or share externally.