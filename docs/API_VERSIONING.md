# API Versioning

This document explains the API versioning strategy implemented in TaskFlow.Api, following Microsoft's official recommendations and industry best practices.

## Overview

TaskFlow.Api uses **URL path versioning** to manage multiple API versions. This approach provides:

- **Clear versioning** - Version is explicitly visible in the URL
- **Easy migration** - Clients can upgrade at their own pace
- **Backward compatibility** - Multiple versions coexist without breaking existing clients
- **Standard compliance** - Follows Microsoft and REST API best practices

## Implementation

### Technology Stack

- **Asp.Versioning.Mvc** (v8.1.0) - Core versioning functionality
- **Asp.Versioning.Mvc.ApiExplorer** (v8.1.0) - API Explorer integration for Swagger/OpenAPI

### Configuration

API versioning is configured in `ApiVersioningServiceExtensions.cs`:

```csharp
services.AddApiVersioning(options =>
{
    // Default version when client doesn't specify
    options.DefaultApiVersion = new ApiVersion(1, 0);
    
    // Assume default version when not specified
    options.AssumeDefaultVersionWhenUnspecified = true;
    
    // Return supported versions in response headers
    options.ReportApiVersions = true;
    
    // Support URL segment and header versioning
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

## Versioning Strategy

### URL Path Versioning (Primary)

The recommended approach for API consumers:

```
GET /api/v1/TaskItems
GET /api/v2/TaskItems
```

**Advantages:**
- Intuitive and discoverable
- Easy to test and debug
- Clear in documentation
- Works with all HTTP clients

### Header Versioning (Fallback)

Alternative method using custom header:

```bash
curl -H "x-api-version: 1.0" http://localhost:5290/api/TaskItems
```

**Use cases:**
- When URL structure must remain constant
- For advanced API consumers
- Testing different versions

### Legacy Endpoints

Endpoints without version prefix continue to work for backward compatibility:

```
GET /api/TaskItems  # Defaults to v1.0
```

These endpoints support version 1.0 and maintain the original API contract.

## Available Versions

### Version 1.0

Current version with core functionality:

- **Endpoints:**
  - `GET /api/v1/TaskItems` - List all tasks
  - `GET /api/v1/TaskItems/{id}` - Get task by ID
  - `POST /api/v1/TaskItems` - Create task
  - `PUT /api/v1/TaskItems/{id}` - Update task
  - `DELETE /api/v1/TaskItems/{id}` - Delete task

- **Response format:**
  ```json
  {
    "id": 1,
    "title": "Task Title",
    "description": "Task Description",
    "isComplete": false
  }
  ```

**Note:** The versioning infrastructure is in place to support future API versions. When new versions are needed, they can be added by creating new controllers with the `[ApiVersion("X.0")]` attribute.

## Using API Versions

### Example: Creating a Task (V1)

```bash
curl -X POST http://localhost:5290/api/v1/TaskItems \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My Task",
    "description": "Task description",
    "isComplete": false
  }'
```

**Response:**
```json
{
  "id": 1,
  "title": "My Task",
  "description": "Task description",
  "isComplete": false
}
```

### Example: Using Header Versioning

```bash
curl -X GET http://localhost:5290/api/TaskItems \
  -H "x-api-version: 1.0"
```

**Response:**
```json
{
  "id": 1,
  "title": "My Task",
  "description": "Task description",
  "isComplete": false
}
```

## API Version Discovery

### Response Headers

All API responses include version information headers:

```
api-supported-versions: 1.0
```

This header lists all supported API versions, helping clients discover available versions.

### Swagger/OpenAPI Documentation

Swagger UI automatically documents all API versions:

- **V1 Documentation:** Select "TaskFlow API V1" in Swagger UI dropdown
- Additional versions will appear as they are added

Access Swagger UI at: `http://localhost:{port}` (in Development mode)

## Adding New Versions

When you need to add a new API version:

1. **Create a new versioned controller** in a new namespace (e.g., `Controllers/V2/`)
2. **Add the ApiVersion attribute**: `[ApiVersion("2.0")]`
3. **Use versioned route**: `[Route("api/v{version:apiVersion}/[controller]")]`
4. **Update route names** to avoid conflicts (e.g., `"GetTaskV2"` instead of `"GetTask"`)
5. **Update tests** to cover the new version

**Example:**
```csharp
namespace TaskFlow.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TaskItemsController : ControllerBase
{
    // Implement V2 endpoints
}
```

### Handling Multiple Versions

**Best practices:**
1. Keep existing versions stable - don't modify them
2. Add new versions for breaking changes
3. Clearly document what changed between versions
4. Deprecate old versions with adequate notice
5. Monitor usage to understand migration patterns

## Best Practices

### For API Consumers

1. **Always specify version** - Use explicit version in URL (`/api/v1/` or `/api/v2/`)
2. **Handle additional fields** - Design clients to ignore unknown fields for forward compatibility
3. **Monitor version headers** - Check `api-supported-versions` header for available versions
4. **Test before migrating** - Thoroughly test your application with new versions
5. **Have a rollback plan** - Keep V1 implementation ready if issues arise

### For API Maintainers

1. **No breaking changes in minor versions** - Only add, never remove or change existing fields
2. **Document all changes** - Clear migration guides for each version
3. **Deprecation warnings** - Announce version deprecation well in advance
4. **Support multiple versions** - Maintain at least two versions during transitions
5. **Monitor usage** - Track which versions are actively used

## Deprecation Policy

When deprecating an API version:

1. **Announce deprecation** - At least 6 months notice
2. **Mark as deprecated** - Use `[ApiVersion("1.0", Deprecated = true)]`
3. **Update documentation** - Clear migration path to current version
4. **Set sunset header** - Indicate when version will be removed
5. **Monitor usage** - Ensure minimal impact before removal

Example deprecation annotation:
```csharp
[ApiVersion("1.0", Deprecated = true)]
public class TaskItemsController : ControllerBase
{
    // ...
}
```

## Technical Architecture

### Controller Organization

```
Controllers/
├── TaskItemsController.cs      # Supports v1.0 (legacy route /api/TaskItems)
└── V1/
    └── TaskItemsController.cs  # Version 1.0 (versioned route /api/v1/TaskItems)
```

Future versions can be added in new namespaces (e.g., `V2/`, `V3/`).

### Version Attributes

```csharp
[ApiVersion("1.0")]                    // Declares controller supports v1.0
[ApiVersion("2.0")]                    // Declares controller supports v2.0
[Route("api/v{version:apiVersion}/[controller]")]  // Versioned URL template
[Route("api/[controller]")]            // Legacy non-versioned route
```

### Swagger Integration

The Swagger UI is automatically configured to:
- Generate separate documentation for each API version
- Group endpoints by version
- Show version-specific schemas
- Provide version selector dropdown

## Testing

### Unit Tests

Version-specific tests validate each controller independently:

- `TaskFlow.Api.Tests/Controllers/V1/TaskItemsControllerV1Tests.cs`
- Additional version tests as new versions are added

### Integration Testing

Test version selection mechanisms:

```csharp
// Test URL versioning
var response = await client.GetAsync("/api/v1/TaskItems");

// Test header versioning
client.DefaultRequestHeaders.Add("x-api-version", "1.0");
var response = await client.GetAsync("/api/TaskItems");

// Verify response headers
Assert.Contains("api-supported-versions", response.Headers.Select(h => h.Key));
```

## Troubleshooting

### Issue: "Unsupported API version" error

**Cause:** Requested version doesn't exist

**Solution:** Check `api-supported-versions` header or Swagger docs for available versions. Currently only v1.0 is supported.

### Issue: Version not specified in URL

**Cause:** Using legacy endpoint without version

**Solution:** For new integrations, use versioned endpoint: `/api/v1/TaskItems`. Legacy endpoints (`/api/TaskItems`) will continue to work for backward compatibility.

## References

- [Microsoft API Versioning GitHub](https://github.com/dotnet/aspnet-api-versioning)
- [Microsoft Learn: API Versioning](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/versioning)
- [Azure API Design Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning)
- [RESTful API Design Guidelines](https://restfulapi.net/versioning/)

## Summary

TaskFlow.Api implements robust API versioning using Microsoft's recommended practices:

✅ URL path versioning as primary method  
✅ Header versioning as fallback  
✅ Infrastructure ready for multiple versions  
✅ Clear documentation for versioning strategy  
✅ Backward compatibility maintained  
✅ Easy path to add new versions  

This approach provides the foundation for API evolution without disrupting existing consumers. Currently, version 1.0 is available, and the infrastructure is in place to add additional versions as needed.

---

*Last updated: 2025-11-05*
