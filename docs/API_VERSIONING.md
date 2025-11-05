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

### Version-Neutral Endpoints

Legacy endpoints without version prefix continue to work:

```
GET /api/TaskItems  # Defaults to v1.0
```

These are marked with `[ApiVersionNeutral]` for backward compatibility.

## Available Versions

### Version 1.0

Base version with core functionality:

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

### Version 2.0

Enhanced version with additional metadata:

- **Endpoints:**
  - `GET /api/v2/TaskItems` - List all tasks with metadata
  - `GET /api/v2/TaskItems/{id}` - Get task by ID with metadata
  - `POST /api/v2/TaskItems` - Create task (returns enhanced response)
  - `PUT /api/v2/TaskItems/{id}` - Update task
  - `DELETE /api/v2/TaskItems/{id}` - Delete task

- **Enhanced response format:**
  ```json
  {
    "id": 1,
    "title": "Task Title",
    "description": "Task Description",
    "isComplete": false,
    "metadata": {
      "apiVersion": "2.0",
      "timestamp": "2025-11-05T03:03:40.5116771Z"
    }
  }
  ```

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

### Example: Creating a Task (V2)

```bash
curl -X POST http://localhost:5290/api/v2/TaskItems \
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
  "isComplete": false,
  "metadata": {
    "apiVersion": "2.0",
    "timestamp": "2025-11-05T03:03:40.5116771Z"
  }
}
```

### Example: Using Header Versioning

```bash
curl -X GET http://localhost:5290/api/TaskItems \
  -H "x-api-version: 2.0"
```

## API Version Discovery

### Response Headers

All API responses include version information headers:

```
api-supported-versions: 1.0, 2.0
```

This header lists all supported API versions, helping clients discover available versions.

### Swagger/OpenAPI Documentation

Each API version has its own Swagger documentation:

- **V1 Documentation:** Select "TaskFlow API V1" in Swagger UI dropdown
- **V2 Documentation:** Select "TaskFlow API V2" in Swagger UI dropdown

Access Swagger UI at: `http://localhost:{port}` (in Development mode)

## Migration Guide

### Upgrading from V1 to V2

**Changes in V2:**
1. Response includes `metadata` object
2. Metadata contains `apiVersion` and `timestamp`
3. All request/response schemas remain compatible

**Migration steps:**
1. Update client code to handle `metadata` in responses (optional)
2. Change URLs from `/api/v1/` to `/api/v2/`
3. Test thoroughly before deploying

**Backward compatibility:**
- V1 endpoints remain fully functional
- No breaking changes in request format
- V2 is additive (adds fields, doesn't remove)

### Handling Multiple Versions

**Recommended approach:**
1. Start with V1 for all clients
2. Test V2 in non-production environments
3. Gradually migrate clients to V2
4. Monitor usage of both versions
5. Plan deprecation of V1 after sufficient adoption

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
├── TaskItemsController.cs      # Version-neutral (legacy)
├── V1/
│   └── TaskItemsController.cs  # Version 1.0
└── V2/
    └── TaskItemsController.cs  # Version 2.0
```

### Version Attributes

```csharp
[ApiVersion("1.0")]                    // Supports v1.0
[ApiVersion("2.0")]                    // Supports v2.0
[ApiVersionNeutral]                    // Version-independent
[Route("api/v{version:apiVersion}/[controller]")]  // URL template
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
- `TaskFlow.Api.Tests/Controllers/V2/TaskItemsControllerV2Tests.cs`

### Integration Testing

Test version selection mechanisms:

```csharp
// Test URL versioning
var response = await client.GetAsync("/api/v2/TaskItems");

// Test header versioning
client.DefaultRequestHeaders.Add("x-api-version", "2.0");
var response = await client.GetAsync("/api/TaskItems");
```

## Troubleshooting

### Issue: "Unsupported API version" error

**Cause:** Requested version doesn't exist

**Solution:** Check `api-supported-versions` header or Swagger docs for available versions

### Issue: Unexpected response format

**Cause:** Using wrong version

**Solution:** Verify URL contains correct version (`/api/v1/` vs `/api/v2/`)

### Issue: Version not specified in URL

**Cause:** Using legacy endpoint without version

**Solution:** Update to versioned endpoint: `/api/v1/TaskItems`

## References

- [Microsoft API Versioning GitHub](https://github.com/dotnet/aspnet-api-versioning)
- [Microsoft Learn: API Versioning](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/versioning)
- [Azure API Design Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning)
- [RESTful API Design Guidelines](https://restfulapi.net/versioning/)

## Summary

TaskFlow.Api implements robust API versioning using Microsoft's recommended practices:

✅ URL path versioning as primary method  
✅ Header versioning as fallback  
✅ Multiple versions coexist safely  
✅ Clear documentation for each version  
✅ Backward compatibility maintained  
✅ Easy migration path for clients  

This approach ensures API evolution without disrupting existing consumers.

---

*Last updated: 2025-11-05*
