# Azure Authentication Methods Comparison

## Overview

This document compares the deprecated `--sdk-auth` method with the modern OIDC (OpenID Connect) approach for Azure authentication in GitHub Actions.

## Authentication Flow Comparison

### Old Method: --sdk-auth (Deprecated ❌)

```
┌─────────────────┐
│  GitHub Secrets │
│                 │
│ AZURE_CREDENTIALS = {
│   "clientId": "...",
│   "clientSecret": "...",  ← Long-lived secret stored in GitHub
│   "subscriptionId": "...",
│   "tenantId": "..."
│ }                │
└─────────────────┘
        ↓
┌─────────────────────────┐
│  GitHub Actions Workflow │
│                         │
│  - name: Azure Login    │
│    with:                │
│      creds: ${{ secrets.AZURE_CREDENTIALS }}
│                         │
└─────────────────────────┘
        ↓
┌─────────────────┐
│  Azure AD       │
│                 │
│  Validates      │
│  client secret  │
│                 │
└─────────────────┘
        ↓
    ✓ Authenticated
```

**Issues:**
- ❌ Client secret stored in GitHub (security risk)
- ❌ Requires manual rotation before expiration
- ❌ Secret can be accidentally exposed in logs
- ❌ No fine-grained control over which workflows can authenticate
- ❌ Deprecated by Azure - will stop working in future releases

### New Method: OIDC with Federated Credentials (Recommended ✅)

```
┌─────────────────┐
│  GitHub Secrets │
│                 │
│ AZURE_CLIENT_ID      (public identifier)
│ AZURE_TENANT_ID      (public identifier)
│ AZURE_SUBSCRIPTION_ID (public identifier)
│                 │
│ (No secrets stored!)
└─────────────────┘
        ↓
┌─────────────────────────┐
│  GitHub Actions Workflow │
│                         │
│  permissions:           │
│    id-token: write      │
│                         │
│  - name: Azure Login    │
│    with:                │
│      client-id: ${{ secrets.AZURE_CLIENT_ID }}
│      tenant-id: ${{ secrets.AZURE_TENANT_ID }}
│      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
│                         │
└─────────────────────────┘
        ↓
┌─────────────────────────┐
│  GitHub OIDC Provider   │
│                         │
│  Issues short-lived     │
│  JWT token for this     │
│  specific workflow run  │
│                         │
└─────────────────────────┘
        ↓
┌─────────────────────────┐
│  Azure AD               │
│                         │
│  Federated Credential   │
│  Configuration:         │
│  - Issuer: GitHub       │
│  - Subject: repo/branch │
│  - Audiences: AzureAD   │
│                         │
│  Validates JWT token    │
│  against configured     │
│  federated credentials  │
└─────────────────────────┘
        ↓
    ✓ Authenticated
    (Token expires in ~1 hour)
```

**Benefits:**
- ✅ No secrets stored in GitHub
- ✅ Tokens are short-lived (automatic rotation)
- ✅ Fine-grained control (specific repos/branches)
- ✅ Better audit trail in Azure
- ✅ Modern, supported approach
- ✅ Meets compliance requirements

## Side-by-Side Comparison

| Feature | --sdk-auth (Old) | OIDC (New) |
|---------|------------------|------------|
| **Secrets stored in GitHub** | Yes (client secret) | No |
| **Token lifetime** | Long-lived (months/years) | Short-lived (~1 hour) |
| **Manual rotation required** | Yes, before expiration | No, automatic |
| **Risk of secret exposure** | High | None |
| **Workflow-specific access** | No | Yes |
| **Repository-specific access** | No | Yes |
| **Branch-specific access** | No | Yes |
| **Audit trail** | Basic | Detailed |
| **Setup complexity** | Simple | Moderate |
| **Azure support status** | Deprecated ❌ | Supported ✅ |
| **Security best practice** | No | Yes |

## Configuration Comparison

### Old Configuration (Deprecated)

**Service Principal Creation:**
```bash
az ad sp create-for-rbac \
  --name "MyApp" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth  # ← Deprecated flag
```

**Output (stored as single GitHub secret):**
```json
{
  "clientId": "00000000-0000-0000-0000-000000000000",
  "clientSecret": "very-secret-string-here",
  "subscriptionId": "00000000-0000-0000-0000-000000000000",
  "tenantId": "00000000-0000-0000-0000-000000000000",
  "activeDirectoryEndpointUrl": "...",
  "resourceManagerEndpointUrl": "...",
  ...
}
```

**GitHub Workflow:**
```yaml
- name: Azure Login
  uses: azure/login@v2
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}
```

**GitHub Secrets Required:** 1
- `AZURE_CREDENTIALS` (full JSON)

---

### New Configuration (OIDC)

**Service Principal Creation:**
```bash
az ad sp create-for-rbac \
  --name "MyApp" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}
  # No --sdk-auth flag!
```

**Output (save these values):**
```json
{
  "appId": "00000000-0000-0000-0000-000000000000",
  "displayName": "MyApp",
  "password": "...",  # ← Not needed for OIDC!
  "tenant": "00000000-0000-0000-0000-000000000000"
}
```

**Federated Credential Configuration:**
```bash
az ad app federated-credential create \
  --id {appId} \
  --parameters '{
    "name": "MyAppGitHubActions",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:myorg/myrepo:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

**GitHub Workflow:**
```yaml
permissions:
  id-token: write  # ← Required for OIDC
  contents: read

jobs:
  deploy:
    steps:
      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

**GitHub Secrets Required:** 3
- `AZURE_CLIENT_ID` (public, not sensitive)
- `AZURE_TENANT_ID` (public, not sensitive)
- `AZURE_SUBSCRIPTION_ID` (public, not sensitive)

## Security Implications

### Old Method Security Risks

1. **Secret Storage**: Client secret stored in GitHub is a single point of compromise
2. **Secret Exposure**: Can be accidentally logged or exposed in error messages
3. **No Expiration**: Secret remains valid until manually rotated
4. **Broad Access**: Any workflow with access to the secret can authenticate
5. **No Repo Binding**: Secret can be used from any repository if compromised

### New Method Security Benefits

1. **No Secret Storage**: GitHub secrets contain only public identifiers
2. **Token-Based**: Each workflow run gets a unique, short-lived token
3. **Auto Expiration**: Tokens expire automatically (~1 hour)
4. **Workflow Binding**: Only specific workflows/branches can authenticate
5. **Repository Binding**: Federated credentials are tied to specific repository

## Migration Impact

### What Changes
- ✅ Authentication method in GitHub Actions workflows
- ✅ GitHub repository secrets (3 new secrets instead of 1)
- ✅ Azure AD configuration (add federated credentials)

### What Stays the Same
- ✅ Azure infrastructure (no changes)
- ✅ Service principal permissions (same roles)
- ✅ Application code (no changes)
- ✅ Deployment process (same steps)

## Recommendation

**Use OIDC with Federated Credentials** for all new deployments and migrate existing deployments as soon as possible. The old `--sdk-auth` method is deprecated and will be removed in future Azure releases.

## Next Steps

1. Review the [Azure OIDC Authentication Guide](AZURE_OIDC_AUTHENTICATION.md)
2. Follow the [Migration Checklist](AZURE_AUTH_MIGRATION_CHECKLIST.md)
3. Test thoroughly before removing old secrets
4. Update team documentation

## Additional Resources

- [GitHub Docs - OIDC in Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Azure Workload Identity Federation](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [Security Best Practices for GitHub Actions](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
