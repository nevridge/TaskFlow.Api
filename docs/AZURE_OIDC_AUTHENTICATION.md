# Azure OIDC Authentication Setup Guide

## Overview

This guide explains how to configure Azure authentication for GitHub Actions using OpenID Connect (OIDC) and federated credentials. This is the modern, recommended approach that replaces the deprecated `--sdk-auth` method.

## Benefits of OIDC Authentication

- **No secrets to manage**: No need to store and rotate service principal credentials
- **Improved security**: Uses short-lived tokens that are automatically rotated
- **Better audit trail**: Azure can track which GitHub workflows access which resources
- **Simpler maintenance**: No need to update secrets when credentials expire
- **Compliance-friendly**: Meets modern security requirements for cloud authentication

## Prerequisites

- Azure subscription with permissions to create service principals and configure federated credentials
- GitHub repository with Actions enabled
- Azure CLI installed (for setup commands)
- Contributor or Owner role on the Azure subscription or resource group

## Setup Instructions

### Step 1: Create an Azure AD Application and Service Principal

First, create a new Azure AD application and service principal:

```bash
# Set variables (customize these for your environment)
APP_NAME="TaskFlowGitHubActions"
SUBSCRIPTION_ID="your-subscription-id"

# Create the Azure AD application and service principal
az ad sp create-for-rbac \
  --name "$APP_NAME" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID
```

**Important**: Save the output from this command. You'll need the following values:
- `appId` - This is your **Client ID**
- `tenant` - This is your **Tenant ID**

**Note**: You can also scope the service principal to specific resource groups for better security:

```bash
# Create service principal scoped to a specific resource group
RESOURCE_GROUP="TaskFlowRG"
az ad sp create-for-rbac \
  --name "$APP_NAME" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP
```

### Step 2: Configure Federated Credentials for GitHub Actions

Now configure the federated credentials that will allow GitHub Actions to authenticate:

#### For Production Deployments (main branch and tags)

```bash
# Get the application object ID
APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv)

# Configure federated credential for the main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsMain",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/YOUR_REPO_NAME:ref:refs/heads/main",
    "description": "GitHub Actions - Main branch",
    "audiences": [
      "api://AzureADTokenExchange"
    ]
  }'

# Configure federated credential for tags (for release deployments)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsTags",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/YOUR_REPO_NAME:ref:refs/tags/v*",
    "description": "GitHub Actions - Version tags",
    "audiences": [
      "api://AzureADTokenExchange"
    ]
  }'
```

#### For Pull Request Deployments (optional)

If you want to enable deployments from pull requests:

```bash
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsPR",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/YOUR_REPO_NAME:pull_request",
    "description": "GitHub Actions - Pull requests",
    "audiences": [
      "api://AzureADTokenExchange"
    ]
  }'
```

#### For Ephemeral/Manual Deployments

For workflows triggered manually or from any branch:

```bash
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "TaskFlowGitHubActionsEnvironment",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_ORG/YOUR_REPO_NAME:environment:production",
    "description": "GitHub Actions - Production environment",
    "audiences": [
      "api://AzureADTokenExchange"
    ]
  }'
```

**Important**: Replace `YOUR_GITHUB_ORG/YOUR_REPO_NAME` with your actual GitHub organization/username and repository name. For example: `nevridge/TaskFlow.Api`

### Step 3: Configure GitHub Repository Secrets

Add the following secrets to your GitHub repository:

1. Go to your GitHub repository
2. Navigate to **Settings → Secrets and variables → Actions**
3. Add the following **repository secrets**:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `AZURE_CLIENT_ID` | The `appId` from Step 1 | Application (client) ID |
| `AZURE_TENANT_ID` | The `tenant` from Step 1 | Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | Your Azure subscription ID | Target subscription |

**Note**: You can find your subscription ID using:

```bash
az account show --query id -o tsv
```

### Step 4: Verify GitHub Workflow Configuration

Ensure your GitHub workflows have the correct permissions and use the OIDC authentication:

```yaml
permissions:
  id-token: write  # Required for OIDC token exchange
  contents: read   # Required for checking out code

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

**Critical**: The `permissions.id-token: write` is required for OIDC authentication to work.

## Subject Claim Patterns

The `subject` field in federated credentials determines which GitHub workflows can authenticate. Here are common patterns:

| Subject Pattern | Description |
|----------------|-------------|
| `repo:ORG/REPO:ref:refs/heads/BRANCH` | Specific branch (e.g., `main`) |
| `repo:ORG/REPO:ref:refs/tags/*` | All tags |
| `repo:ORG/REPO:ref:refs/tags/v*` | Version tags only (v1.0.0, etc.) |
| `repo:ORG/REPO:pull_request` | All pull requests |
| `repo:ORG/REPO:environment:ENV_NAME` | Specific environment |

## Troubleshooting

### Error: "AADSTS70021: No matching federated identity record found"

**Cause**: The federated credential configuration doesn't match the GitHub workflow context.

**Solutions**:
1. Verify the `subject` claim matches your repository and branch/tag pattern
2. Check that you used the correct format: `repo:ORG/REPO:ref:refs/heads/BRANCH`
3. Ensure the federated credential has been fully propagated (can take a few minutes)

**Check your subject claim**:

```bash
# List all federated credentials for the app
APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv)
az ad app federated-credential list --id $APP_ID
```

### Error: "ClientAuthenticationFailed"

**Cause**: The workflow doesn't have the required permissions.

**Solution**: Ensure your workflow has `id-token: write` permission:

```yaml
permissions:
  id-token: write
  contents: read
```

### Error: "The subscription ... could not be found"

**Cause**: The service principal doesn't have access to the specified subscription.

**Solution**: Verify the service principal role assignment:

```bash
# List role assignments for the service principal
az role assignment list --assignee $APP_ID --output table
```

If missing, add the role assignment:

```bash
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID
```

### Testing Authentication

Test your OIDC authentication locally using the GitHub Actions workflow:

1. Trigger your workflow manually or via a push/tag
2. Check the workflow logs for the "Azure Login" step
3. Verify that `az account show` displays the correct subscription

You can also test the Azure CLI authentication in your local environment using:

```bash
# This simulates what happens in GitHub Actions
az login --service-principal \
  --username $APP_ID \
  --tenant $TENANT_ID \
  --federated-token "$(cat token.txt)"  # Token would come from GitHub in Actions
```

## Security Best Practices

1. **Scope service principals appropriately**: Grant only the minimum permissions needed
   - Use resource group scopes instead of subscription-wide access when possible
   - Use specific roles (e.g., `Website Contributor`) instead of `Contributor`

2. **Use environment protection rules**: Configure GitHub environment protection rules for production deployments
   - Require manual approval for production deployments
   - Limit which branches can deploy to production

3. **Rotate credentials regularly**: Even though OIDC uses short-lived tokens, review and rotate the service principal periodically

4. **Monitor authentication**: Use Azure AD audit logs to monitor service principal usage
   ```bash
   # View sign-in logs for the service principal
   az monitor activity-log list \
     --resource-id /subscriptions/$SUBSCRIPTION_ID \
     --caller $APP_ID
   ```

5. **Use separate service principals per environment**: Consider using different service principals for production and non-production environments

## Migration from --sdk-auth

If you're migrating from the deprecated `--sdk-auth` approach:

### Before (Deprecated)

```yaml
- name: Azure Login
  uses: azure/login@v2
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}
```

Where `AZURE_CREDENTIALS` was a JSON object containing:
```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "..."
}
```

### After (OIDC)

```yaml
permissions:
  id-token: write
  contents: read

# ...

- name: Azure Login
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### Migration Steps

1. Follow the setup instructions above to create federated credentials
2. Add the new GitHub secrets (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`)
3. Update your workflows to use OIDC authentication
4. Test the workflows thoroughly
5. After confirming everything works, delete the old `AZURE_CREDENTIALS` secret
6. Consider deleting the old client secret from Azure AD (if you created a new service principal)

## Additional Resources

- [GitHub Actions - Configuring OpenID Connect in Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Azure Documentation - Workload identity federation](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [Azure/login Action Documentation](https://github.com/Azure/login)
- [Azure CLI Reference - Federated Credentials](https://learn.microsoft.com/en-us/cli/azure/ad/app/federated-credential)

## Support

If you encounter issues with OIDC authentication:

1. Check the GitHub Actions workflow logs for detailed error messages
2. Review the troubleshooting section above
3. Verify your federated credential configuration in Azure AD
4. Ensure your service principal has the required permissions
5. Check that the workflow has the required permissions (`id-token: write`)
