# Azure Resource Naming Convention

> **📖 Reference Documentation** - This document details Azure resource naming standards. For deployment instructions, see the [Deployment Guide](DEPLOYMENT.md).


This document defines the standardized naming convention for all Azure resources deployed by TaskFlow.Api workflows.

## Overview

Consistent resource naming improves:
- **Discoverability**: Easy to identify resources and their purpose
- **Automation**: Predictable names enable scripting and automation
- **Cost Management**: Clear resource ownership and environment tracking
- **Compliance**: Adherence to organizational standards

## Naming Convention

All resources follow the pattern: `{org}-{app}-{env}-{resourceType}` with the following components:

- **{org}**: Organization identifier (`nevridge`)
- **{app}**: Application name (`taskflow`)
- **{env}**: Environment (`prod`, `qa`, `dev`)
- **{resourceType}**: Resource type identifier (see table below)

### Resource Type Suffixes

| Resource Type | Suffix/Pattern | Example |
|---------------|----------------|---------|
| Resource Group | `-rg` | `nevridge-taskflow-prod-rg` |
| Azure Container Instance | `-aci` | `nevridge-taskflow-qa-aci` |
| App Service | `-web` | `nevridge-taskflow-prod-web` |
| App Service Plan | `-plan` | `nevridge-taskflow-prod-plan` |
| Container Registry | `{org}{app}{env}acr` | `nevridgetaskflowprodacr` |
| Storage Account | `{org}{app}{env}st{nn}` | `nevridgetaskflowqast01` |

### Special Cases

#### Container Registry (ACR)
ACR names must be globally unique and can only contain alphanumeric characters (no hyphens). Pattern: `{org}{app}{env}acr`

**Examples:**
- Production: `nevridgetaskflowprodacr`
- QA: `nevridgetaskflowqaacr`

#### Storage Accounts
Storage account names must be globally unique, 3-24 characters, lowercase alphanumeric only. Pattern: `{org}{app}{env}st{nn}` where {nn} is a two-digit sequence number.

**Examples:**
- Production: `nevridgetaskflowprodst01`
- QA: `nevridgetaskflowqast01`

#### DNS Labels
DNS name labels for Azure Container Instances should be predictable for QA/testing environments. Pattern: `{app}-{env}`

**Examples:**
- QA: `taskflow-qa` → Full DNS: `taskflow-qa.{region}.azurecontainer.io`
- Staging: `taskflow-staging` → Full DNS: `taskflow-staging.{region}.azurecontainer.io`

## Azure Naming Constraints

### General Rules
- **Length**: Most resources support 1-80 characters
- **Characters**: Alphanumeric, hyphens, and underscores (varies by resource type)
- **Case**: Some resources are case-insensitive (e.g., storage accounts, ACR)

### Resource-Specific Constraints

| Resource Type | Length | Valid Characters | Case | Globally Unique |
|---------------|--------|-----------------|------|-----------------|
| Resource Group | 1-90 | Alphanumeric, underscore, hyphen, period, parentheses | Insensitive | Within subscription |
| ACI | 1-63 | Alphanumeric, hyphen | Insensitive | Within resource group |
| ACR | 5-50 | Alphanumeric only | Insensitive | Global |
| DNS Label | 1-63 | Alphanumeric, hyphen (not at start/end) | Insensitive | Per region |
| App Service | 2-60 | Alphanumeric, hyphen | Insensitive | Global |
| App Service Plan | 1-40 | Alphanumeric, hyphen | Insensitive | Within resource group |
| Storage Account | 3-24 | Lowercase alphanumeric only | Insensitive | Global |

## Workflow Implementation

### Environment Variables

Both production and QA workflows define the following centralized environment variables:

```yaml
env:
  ORG_NAME: nevridge
  APP_NAME: taskflow
  ENV: prod  # or 'qa' for QA workflow
  LOCATION: eastus
```

### Computed Resource Names

Workflows compute resource names from these base variables:

```yaml
- name: Compute resource names
  run: |
    # Base components
    ORG="${{ env.ORG_NAME }}"
    APP="${{ env.APP_NAME }}"
    ENV="${{ env.ENV }}"
    
    # Computed names
    RG_NAME="${ORG}-${APP}-${ENV}-rg"
    ACI_NAME="${ORG}-${APP}-${ENV}-aci"
    DNS_NAME_LABEL="${APP}-${ENV}"
    WEB_NAME="${ORG}-${APP}-${ENV}-web"
    PLAN_NAME="${ORG}-${APP}-${ENV}-plan"
    
    # Special: ACR (no hyphens, alphanumeric only)
    ACR_NAME="${ORG}${APP}${ENV}acr"
    
    # Export to environment
    echo "RG_NAME=$RG_NAME" >> $GITHUB_ENV
    echo "ACI_NAME=$ACI_NAME" >> $GITHUB_ENV
    echo "DNS_NAME_LABEL=$DNS_NAME_LABEL" >> $GITHUB_ENV
    echo "ACR_NAME=$ACR_NAME" >> $GITHUB_ENV
    echo "WEB_NAME=$WEB_NAME" >> $GITHUB_ENV
    echo "PLAN_NAME=$PLAN_NAME" >> $GITHUB_ENV
    
    # Log computed names
    echo "Computed resource names:"
    echo "  Resource Group: $RG_NAME"
    echo "  ACI Container: $ACI_NAME"
    echo "  DNS Label: $DNS_NAME_LABEL"
    echo "  ACR: $ACR_NAME"
    echo "  Web App: $WEB_NAME"
    echo "  App Service Plan: $PLAN_NAME"
```

### Validation Step

Workflows include a validation step to check Azure naming constraints:

```yaml
- name: Validate resource names
  run: |
    # Function to validate length
    validate_length() {
      local name=$1
      local min=$2
      local max=$3
      local resource=$4
      local len=${#name}
      if [ $len -lt $min ] || [ $len -gt $max ]; then
        echo "ERROR: $resource name '$name' length ($len) must be between $min and $max characters"
        exit 1
      fi
    }
    
    # Function to validate characters
    validate_alphanumeric() {
      local name=$1
      local resource=$2
      if ! [[ "$name" =~ ^[a-z0-9]+$ ]]; then
        echo "ERROR: $resource name '$name' must contain only lowercase alphanumeric characters"
        exit 1
      fi
    }
    
    # Validate ACR name
    validate_length "$ACR_NAME" 5 50 "ACR"
    validate_alphanumeric "$ACR_NAME" "ACR"
    
    # Validate DNS label
    validate_length "$DNS_NAME_LABEL" 1 63 "DNS Label"
    if ! [[ "$DNS_NAME_LABEL" =~ ^[a-z0-9]([a-z0-9-]*[a-z0-9])?$ ]]; then
      echo "ERROR: DNS Label '$DNS_NAME_LABEL' must start and end with alphanumeric, can contain hyphens"
      exit 1
    fi
    
    echo "All resource names passed validation"
```

## QA Environment Specifics

### Fixed DNS Label

The QA environment uses a fixed DNS label (`taskflow-qa`) to provide a predictable endpoint for testing:

- **DNS Label**: `taskflow-qa`
- **Full DNS**: `taskflow-qa.eastus.azurecontainer.io` (example for East US)
- **API Endpoint**: `http://taskflow-qa.eastus.azurecontainer.io:8080`
- **Health Check**: `http://taskflow-qa.eastus.azurecontainer.io:8080/health`

### Pre-deployment Cleanup

The QA workflow includes a pre-deployment cleanup step to remove existing containers with the same DNS label:

```yaml
- name: Delete existing ACI if it exists
  run: |
    echo "Checking for existing ACI container: $ACI_NAME"
    if az container show -g $RG_NAME -n $ACI_NAME >/dev/null 2>&1; then
      echo "Deleting existing container to free DNS label..."
      az container delete -g $RG_NAME -n $ACI_NAME --yes
      # Wait for deletion to complete
      for i in {1..30}; do
        if ! az container show -g $RG_NAME -n $ACI_NAME >/dev/null 2>&1; then
          echo "Container deletion confirmed"
          break
        fi
        sleep 2
      done
    else
      echo "No existing container found"
    fi
```

## Production Environment Specifics

### Web App Naming

Production uses Azure App Service (Web Apps) instead of ACI:

- **Resource Group**: `nevridge-taskflow-prod-rg`
- **App Service Plan**: `nevridge-taskflow-prod-plan`
- **Web App**: `nevridge-taskflow-prod-web`
- **ACR**: `nevridgetaskflowprodacr`
- **Public URL**: `https://nevridge-taskflow-prod-web.azurewebsites.net`

## Migration from Legacy Names

If you are migrating from legacy resource names, follow these steps:

### Option 1: Clean Deployment (Recommended for QA/Dev)

1. Delete existing resources:
   ```bash
   az group delete -n OldResourceGroup --yes
   ```

2. Run workflow with new naming convention

### Option 2: Gradual Migration (Recommended for Production)

1. Create new resources alongside old ones
2. Test thoroughly with new resources
3. Update DNS/routing to point to new resources
4. Decommission old resources after validation

### Example Legacy to New Mapping

| Old Name | New Name | Resource Type |
|----------|----------|---------------|
| `TaskFlowRG` | `nevridge-taskflow-prod-rg` | Resource Group |
| `taskflowapi2074394909` | `nevridge-taskflow-prod-web` | Web App |
| `taskflowregistry` | `nevridgetaskflowprodacr` | ACR |
| `taskflowapi` | `taskflowapi` | ACR Image Name |
| `TaskFlowAppServicePlan` | `nevridge-taskflow-prod-plan` | App Service Plan |
| `taskflow-qa` (ACI) | `nevridge-taskflow-qa-aci` | Container Instance |

## Best Practices

1. **Always use computed names**: Never hard-code resource names directly in workflow steps
2. **Log computed names**: Always log the computed names at the start of deployment for debugging
3. **Validate early**: Run validation before creating any resources to fail fast
4. **Document changes**: Update this document when adding new resource types
5. **Test in QA**: Always test naming changes in QA environment before applying to production
6. **Preserve DNS labels**: For QA environments, keep DNS labels consistent across deployments

## References

- [Azure Naming Rules and Restrictions](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules)
- [Azure Cloud Adoption Framework - Naming Convention](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging)
- [Azure Container Instances Documentation](https://learn.microsoft.com/en-us/azure/container-instances/)
- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)

## Workflow Files

- Production: `.github/workflows/prod-deploy.yaml`
- QA: `.github/workflows/qa-deploy.yaml`

## Related Documentation

- [QA Deployment Guide](./QA_DEPLOYMENT.md)
- [Main README](../README.md)

---

[← Back to Documentation Index](README.md) | [Deployment Guide](DEPLOYMENT.md)
