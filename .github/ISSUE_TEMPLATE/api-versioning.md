---
name: Add API Versioning Support
about: Implement API versioning to support backward compatibility and evolutionary changes
title: '[FEATURE] Add API Versioning Support'
labels: enhancement, api
assignees: ''

---

## Summary

Add API versioning support to TaskFlow.Api to enable backward compatibility and allow the API to evolve without breaking existing clients.

## Problem Statement

Currently, TaskFlow.Api does not implement API versioning. This means:

1. **Breaking changes** require all clients to update simultaneously
2. **No backward compatibility** for clients using older API versions
3. **Difficult migration path** when introducing new features or changes
4. **Limited flexibility** in evolving the API over time

As the API grows and is consumed by more clients, versioning becomes essential for:
- Supporting multiple client versions simultaneously
- Providing a clear deprecation path for older API versions
- Enabling gradual rollout of new features
- Maintaining a professional, production-ready API

## Proposed Solution

Implement API versioning using ASP.NET Core API Versioning libraries with support for multiple versioning strategies.

### Recommended Approach: URL Path Versioning

**Primary Strategy:** URL-based versioning (most common and RESTful)
- Format: `/api/v{version}/TaskItems`
- Example: `/api/v1/TaskItems`, `/api/v2/TaskItems`

**Benefits:**
- Clear and explicit version identification
- Easy to test and debug
- Browser-friendly (can be tested directly in browser)
- Standard industry practice
- Works with all HTTP clients
- Good for documentation and API discoverability

### Alternative Approaches (Optional)

1. **Header-based Versioning**
   - Custom header: `api-version: 1.0`
   - Cleaner URLs but requires header configuration

2. **Query String Versioning**
   - Format: `/api/TaskItems?api-version=1.0`
   - Simple but less clean URLs

3. **Media Type Versioning**
   - Accept header: `application/json;version=1.0`
   - Most RESTful but complex for clients

## Implementation Details

### Required NuGet Packages

```xml
<PackageReference Include="Asp.Versioning.Mvc" Version="9.0.0" />
<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="9.0.0" />
```

### Configuration (Program.cs)

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Controller Updates

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TaskItemsController : ControllerBase
{
    // Existing endpoints remain unchanged
    // GET: api/v1/TaskItems
    // POST: api/v1/TaskItems
    // etc.
}
```

### Swagger/OpenAPI Integration

Update Swagger configuration to support versioned API documentation:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(
            description.GroupName,
            new OpenApiInfo
            {
                Title = $"TaskFlow API {description.ApiVersion}",
                Version = description.ApiVersion.ToString()
            });
    }
});
```

## Acceptance Criteria

- [ ] API versioning is implemented with URL path versioning as the primary strategy
- [ ] Existing endpoints are accessible at `/api/v1/TaskItems` (v1.0)
- [ ] Version 1.0 is set as the default when no version is specified
- [ ] API version information is included in response headers (`api-supported-versions`)
- [ ] Swagger/OpenAPI documentation shows separate docs for each version
- [ ] All existing tests pass without modification
- [ ] New tests validate version routing and behavior
- [ ] Documentation is updated with versioning examples
- [ ] Health check endpoints remain unversioned (e.g., `/health`, `/health/ready`)

## Benefits

1. **Backward Compatibility**: Existing clients continue to work with v1 endpoints
2. **Gradual Migration**: New versions can be introduced incrementally
3. **Deprecation Strategy**: Old versions can be marked deprecated and eventually removed
4. **Multiple Versions**: Support multiple API versions simultaneously
5. **Professional Standard**: Follows industry best practices for API design
6. **Client Flexibility**: Clients can upgrade at their own pace

## Example API Evolution

### Version 1.0 (Current)
```
GET  /api/v1/TaskItems
POST /api/v1/TaskItems
PUT  /api/v1/TaskItems/{id}
DELETE /api/v1/TaskItems/{id}
```

### Version 2.0 (Future Example)
```
GET  /api/v2/TaskItems          # Enhanced with pagination
POST /api/v2/TaskItems          # New validation rules
PUT  /api/v2/TaskItems/{id}     # Additional fields
DELETE /api/v2/TaskItems/{id}   # Soft delete support
GET  /api/v2/TaskItems/archived # New endpoint
```

## Breaking Changes Handling

With versioning in place, breaking changes can be introduced in new versions:

1. **Field Renaming**: `IsComplete` → `Status` in v2
2. **Response Structure**: Paginated responses in v2
3. **Authentication**: New auth requirements in v2
4. **Validation Rules**: Stricter validation in v2

## Testing Strategy

1. **Unit Tests**: Verify version attribute behavior
2. **Integration Tests**: Test endpoint routing for each version
3. **Swagger Tests**: Validate multiple version documentation
4. **Header Tests**: Verify `api-supported-versions` response header
5. **Default Version**: Test behavior when version is not specified

## Documentation Updates

- [ ] Update README.md with versioning information
- [ ] Update docs/API.md with versioned endpoint examples
- [ ] Add docs/API_VERSIONING.md with detailed versioning guide
- [ ] Update Swagger UI instructions for version selection
- [ ] Document version deprecation process

## Implementation Phases

### Phase 1: Foundation (Minimal Changes)
1. Add NuGet packages
2. Configure API versioning in `Program.cs`
3. Update controller route to include version segment
4. Mark existing controller as `[ApiVersion("1.0")]`
5. Update Swagger configuration

### Phase 2: Enhancement
1. Add response headers for supported versions
2. Create comprehensive versioning documentation
3. Add integration tests for versioning

### Phase 3: Future (Example)
1. Introduce v2 with enhanced features
2. Deprecate v1 (if needed)
3. Remove v1 after deprecation period

## Non-Goals (Out of Scope)

- Creating a second version (v2) - this issue focuses only on infrastructure
- Implementing sunset policies for old versions
- Adding version negotiation logic beyond defaults
- Breaking changes to existing v1 endpoints

## References

- [ASP.NET Core API Versioning](https://github.com/dotnet/aspnet-api-versioning)
- [Microsoft API Versioning Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning-a-restful-web-api)
- [RESTful API Versioning Strategies](https://www.baeldung.com/rest-versioning)

## Related Issues

None

## Priority

**Medium** - While not urgent, versioning is an important foundation for API evolution and should be implemented before significant API changes are made.

## Estimated Effort

**Small to Medium** (2-4 hours)
- Package installation and configuration: 30 minutes
- Controller updates: 30 minutes
- Swagger/OpenAPI updates: 1 hour
- Testing: 1 hour
- Documentation: 1 hour

## Notes

- This change is **backward compatible** - existing unversioned endpoints can still work
- Default version (1.0) can be applied automatically for unversioned requests
- Health check endpoints should remain unversioned for container orchestration compatibility
- Consider API versioning early to avoid technical debt
