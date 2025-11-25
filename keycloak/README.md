# Keycloak Configuration

## ⚠️ SECURITY WARNING - LOCAL DEVELOPMENT ONLY

This directory contains Keycloak realm configuration files for **LOCAL DEVELOPMENT ONLY**.

### Hardcoded Secrets

The `breast-cancer-realm.json` file contains a hardcoded client secret:
- **Client**: `breast-cancer-api`
- **Secret**: `7ta-la-yter-eldo5an`

**🚨 DO NOT USE THIS CONFIGURATION IN PRODUCTION 🚨**

### Before Production Deployment

1. **Generate a new secure client secret** using Keycloak admin console or CLI
2. **Never commit production secrets** to version control
3. **Use environment variables or secret management systems** (e.g., Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
4. **Rotate secrets regularly** as part of your security practices

### Updating the Client Secret

For production environments:
1. Log into Keycloak Admin Console
2. Navigate to: Realm Settings → Clients → breast-cancer-api → Credentials
3. Click "Regenerate Secret"
4. Store the new secret securely in your secret management system
5. Update your application configuration to use the new secret via environment variables

### Recommended Production Setup

```bash
# Use environment variables instead of hardcoded values
export KEYCLOAK_CLIENT_SECRET="your-secure-production-secret"
```

Then reference it in your application configuration (appsettings.json):
```json
{
  "Keycloak": {
    "ClientSecret": "${KEYCLOAK_CLIENT_SECRET}"
  }
}
```
