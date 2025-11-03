# GitHub Secrets and Environment Variables Audit

**Date:** 2025-11-03  
**Repository:** nevridge/TaskFlow.Api

## Executive Summary

This document provides a comprehensive audit of GitHub repository secrets and environment variables to identify unused items that can be safely removed.

## GitHub Repository Secrets Analysis

### Secrets Currently Configured

The following secrets are configured in the GitHub repository:

1. `ACR_LOGIN_SERVER`
2. `ACR_PASSWORD`
3. `ACR_USERNAME`
4. `AZURE_CLIENT_ID`
5. `AZURE_CREDENTIALS`
6. `AZURE_RESOURCE_GROUP`
7. `AZURE_SUBSCRIPTION_ID`
8. `AZURE_TENANT_ID`
9. `AZURE_WEBAPP_NAME`

### Secret Usage Analysis

#### ✅ Secrets Currently In Use

The following secrets are **actively referenced** in workflows:

| Secret Name | Used In | Purpose |
|------------|---------|---------|
| `AZURE_CLIENT_ID` | `prod-deploy.yaml`, `qa-deploy.yaml` | OpenID Connect authentication for Azure login |
| `AZURE_TENANT_ID` | `prod-deploy.yaml`, `qa-deploy.yaml` | Azure tenant identification for OIDC auth |
| `AZURE_SUBSCRIPTION_ID` | `prod-deploy.yaml`, `qa-deploy.yaml` | Azure subscription for OIDC auth |
| `AZURE_CREDENTIALS` | `prod-teardown.yaml` | Service principal credentials for Azure login (legacy format) |

#### ❌ Unused Secrets (Safe to Delete)

The following secrets are **NOT referenced anywhere** in workflows or application code and can be safely deleted:

1. **`ACR_LOGIN_SERVER`** - Not used; ACR login server is dynamically computed from naming convention
2. **`ACR_PASSWORD`** - Not used; ACR authentication now uses managed identity or dynamic credentials retrieval
3. **`ACR_USERNAME`** - Not used; ACR authentication now uses managed identity or dynamic credentials retrieval
4. **`AZURE_RESOURCE_GROUP`** - Not used; resource group names are dynamically computed from naming convention
5. **`AZURE_WEBAPP_NAME`** - Not used; web app names are dynamically computed from naming convention

### Migration Notes

The workflows have transitioned to:
- **Computed resource names** based on standardized naming conventions (`{org}-{app}-{env}-{type}`)
- **OpenID Connect (OIDC)** authentication for production and QA deployments
- **Managed identity** for ACR pull permissions in production
- **Dynamic credential retrieval** using Azure CLI for ACR authentication in QA

The legacy secrets (ACR_*, AZURE_RESOURCE_GROUP, AZURE_WEBAPP_NAME) were likely used in older workflow versions but are now obsolete.

### Special Case: AZURE_CREDENTIALS

`AZURE_CREDENTIALS` is used only in `prod-teardown.yaml` for service principal authentication. Consider:
- Migrating `prod-teardown.yaml` to use OIDC authentication (like prod-deploy and qa-deploy)
- Once migrated, `AZURE_CREDENTIALS` can also be removed

## Environment Variables Analysis

### Environment Variables in Application

The application uses the following environment variables:

#### Standard ASP.NET Core Variables
- `ASPNETCORE_ENVIRONMENT` - Standard framework variable for environment detection
- `ASPNETCORE_URLS` - Standard framework variable for binding URLs
- `ASPNETCORE_HTTP_PORTS` - Standard framework variable for HTTP port configuration
- `DOTNET_RUNNING_IN_CONTAINER` - Standard framework variable for container detection

#### Application-Specific Variables
- `LOG_PATH` - Optional override for log file location (defaults to `/app/logs/log.txt`)

#### Configuration-Based Variables
- `Database__MigrateOnStartup` - Controls automatic EF Core migrations (set via configuration, can be environment variable)
- `ConnectionStrings__DefaultConnection` - Database connection string (typically set via appsettings.json)

### Environment Variables Status

✅ **All environment variables referenced in the application are actively used.** No unused environment variables were identified.

The environment variables are properly scoped:
- Docker Compose files define container-specific variables
- launchSettings.json defines development-time variables
- No hardcoded secrets or credentials found in environment variables

## Recommendations

### Immediate Actions

1. **Delete the following unused GitHub secrets:**
   - `ACR_LOGIN_SERVER`
   - `ACR_PASSWORD`
   - `ACR_USERNAME`
   - `AZURE_RESOURCE_GROUP`
   - `AZURE_WEBAPP_NAME`

### Future Improvements

2. **Migrate prod-teardown.yaml to OIDC:**
   - Update `prod-teardown.yaml` to use OIDC authentication (matching prod-deploy and qa-deploy)
   - Once migrated, delete `AZURE_CREDENTIALS` secret

3. **Documentation:**
   - Document the required secrets in the README or deployment guide
   - Document the naming convention used for resource name computation

## Conclusion

- **5 GitHub secrets** can be safely deleted immediately
- **0 unused environment variables** were found - all are actively used
- The repository has successfully transitioned to modern authentication patterns (OIDC, managed identity)
- No security concerns identified regarding credential management
