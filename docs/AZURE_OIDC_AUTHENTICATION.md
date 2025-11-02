# Azure OIDC Authentication Setup

This guide covers how to set up Azure authentication for TaskFlow.Api GitHub Actions using OpenID Connect (OIDC) with federated credentials. This replaces the deprecated `--sdk-auth` method and eliminates the need to store client secrets.

## Prerequisites

- Azure subscription with permissions to create service principals
- Azure CLI installed
- Contributor or Owner role on the Azure subscription or resource group

## Quick Setup

### 1. Create Service Principal

```bash
az ad sp create-for-rbac \
  --name "TaskFlowGitHubActions" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID
```

Save the output - you'll need `appId` (client ID) and `tenant` (tenant ID).

### 2. Configure Federated Credentials

Replace `nevridge/TaskFlow.Api` with your repository if different:

```bash
APP_ID=$(az ad app list --display-name "TaskFlowGitHubActions" --query "[0].appId" -o tsv)

# For main branch deployments
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "TaskFlowMain",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:nevridge/TaskFlow.Api:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

# For version tag releases (v*)
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "TaskFlowTags",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:nevridge/TaskFlow.Api:ref:refs/tags/v*",
  "audiences": ["api://AzureADTokenExchange"]
}'

# For production environment (manual/ephemeral workflows)
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "TaskFlowProduction",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:nevridge/TaskFlow.Api:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

### 3. Add GitHub Secrets

In GitHub repository **Settings → Secrets and variables → Actions**, add:

- `AZURE_CLIENT_ID` - The `appId` from step 1
- `AZURE_TENANT_ID` - The `tenant` from step 1
- `AZURE_SUBSCRIPTION_ID` - Run `az account show --query id -o tsv`

### 4. Verify Workflows

The workflows in this repository already have the correct configuration:
- `permissions.id-token: write` is set (required for OIDC)
- `azure/login@v2` uses individual parameters (not JSON creds)

## Common Issues

**"AADSTS70021: No matching federated identity record found"**
- Verify the subject claim matches your repository name exactly
- Check federated credentials: `az ad app federated-credential list --id $APP_ID`

**"ClientAuthenticationFailed"**
- Ensure workflow has `permissions.id-token: write`

**"The subscription ... could not be found"**
- Verify service principal has Contributor role: `az role assignment list --assignee <your-app-id>`

## Additional Resources

- [GitHub Docs - OIDC in Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Azure Workload Identity Federation](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
