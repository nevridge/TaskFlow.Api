# Azure Authentication Migration Checklist

This checklist will help you migrate from the deprecated `--sdk-auth` authentication method to OIDC with federated credentials.

## Prerequisites
- [ ] Azure CLI installed locally
- [ ] Access to Azure subscription with permissions to create service principals
- [ ] Admin access to GitHub repository settings

## Migration Steps

### 1. Create Service Principal with OIDC Support
- [ ] Open a terminal with Azure CLI authenticated
- [ ] Run the command to create a service principal (see [AZURE_OIDC_AUTHENTICATION.md](AZURE_OIDC_AUTHENTICATION.md) for detailed instructions)
- [ ] Save the output containing `appId` (Client ID) and `tenant` (Tenant ID)
- [ ] Note your subscription ID (run `az account show --query id -o tsv`)

### 2. Configure Federated Credentials

Configure federated credentials for the GitHub Actions workflows that need Azure access:

#### For Production Deployments (deploy.yaml)
- [ ] Create federated credential for `main` branch
- [ ] Create federated credential for version tags (`v*`)
- [ ] (Optional) Create federated credential for production environment

#### For Ephemeral/QA Deployments (ephemeral-deploy.yaml)
- [ ] Create federated credential for manual workflow dispatch
- [ ] Configure subject claim to match your repository and workflow trigger

**Note**: See [AZURE_OIDC_AUTHENTICATION.md](AZURE_OIDC_AUTHENTICATION.md) for the exact Azure CLI commands.

### 3. Update GitHub Repository Secrets

- [ ] Go to GitHub repository **Settings → Secrets and variables → Actions**
- [ ] Click **New repository secret** and add:
  - [ ] `AZURE_CLIENT_ID` = Application (client) ID from step 1
  - [ ] `AZURE_TENANT_ID` = Directory (tenant) ID from step 1
  - [ ] `AZURE_SUBSCRIPTION_ID` = Your Azure subscription ID
- [ ] Verify all three secrets are added correctly

### 4. Test the Workflows

#### Test Production Deployment Workflow
- [ ] Manually trigger the **Deploy to Azure Production** workflow from the Actions tab
- [ ] Verify the "Azure Login" step completes successfully
- [ ] Check that subsequent Azure CLI commands work correctly
- [ ] Verify the deployment completes successfully

#### Test Ephemeral Deployment Workflow
- [ ] Manually trigger the **Ephemeral ACI deploy** workflow with action=deploy
- [ ] Verify the "Azure login" step completes successfully
- [ ] Check that the deployment completes successfully
- [ ] Clean up by running the workflow with action=teardown

### 5. Verify and Clean Up

- [ ] Confirm both workflows can authenticate successfully
- [ ] Verify no authentication errors in workflow logs
- [ ] Test tag-based deployment (create a test tag like `v0.0.1-test`)
- [ ] Once everything is working, delete the old `AZURE_CREDENTIALS` secret from GitHub
- [ ] (Optional) If you created a new service principal, consider removing the old one or rotating its client secret

### 6. Document for Team

- [ ] Inform team members about the authentication change
- [ ] Share the [AZURE_OIDC_AUTHENTICATION.md](AZURE_OIDC_AUTHENTICATION.md) documentation
- [ ] Update any internal documentation or runbooks
- [ ] Update any CI/CD documentation that references the old authentication method

## Rollback Plan (If Needed)

If you encounter issues and need to temporarily rollback:

1. **Keep the old secret**: Don't delete `AZURE_CREDENTIALS` until fully verified
2. **Revert workflow files**: Use git to revert `.github/workflows/deploy.yaml` and `.github/workflows/ephemeral-deploy.yaml` to use `creds: ${{ secrets.AZURE_CREDENTIALS }}`
3. **Troubleshoot**: Review the [Troubleshooting section](AZURE_OIDC_AUTHENTICATION.md#troubleshooting) in the OIDC documentation
4. **Get help**: Check GitHub Actions logs for detailed error messages

## Troubleshooting Common Issues

### "AADSTS70021: No matching federated identity record found"
- **Solution**: Verify the federated credential subject claim matches your repository and branch/tag pattern
- **Check**: Run `az ad app federated-credential list --id $APP_ID` to view configured credentials

### "ClientAuthenticationFailed"
- **Solution**: Ensure workflow has `id-token: write` permission (already configured in both workflows)
- **Check**: Verify all three secrets are set correctly in GitHub

### "The subscription ... could not be found"
- **Solution**: Verify the service principal has Contributor role on the subscription
- **Check**: Run `az role assignment list --assignee $APP_ID --output table`

## Additional Resources

- [Azure OIDC Authentication Guide](AZURE_OIDC_AUTHENTICATION.md) - Complete setup and troubleshooting guide
- [GitHub Docs - Configuring OpenID Connect in Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Azure Workload Identity Federation](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)

## Questions?

If you encounter issues not covered in this checklist or the documentation, please:
1. Check the [Troubleshooting section](AZURE_OIDC_AUTHENTICATION.md#troubleshooting) in the OIDC documentation
2. Review GitHub Actions workflow logs for detailed error messages
3. Open an issue in the repository with details about the error
