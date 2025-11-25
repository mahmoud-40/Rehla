# Keycloak Configuration

## ⚠️ SECURITY WARNING - LOCAL DEVELOPMENT ONLY

This directory contains Keycloak realm configuration files for **LOCAL DEVELOPMENT ONLY**.

### Hardcoded Secrets

The `breast-cancer-realm.json` file contains a hardcoded client secret:
- **Client**: `breast-cancer-api`
- **Secret**: `7ta-la-***-eldo5an` (obfuscated for documentation)

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

For .NET applications, use environment variables or Azure App Configuration:

```bash
# Set environment variable
export Keycloak__ClientSecret="your-secure-production-secret"
```

Or use User Secrets for local development:
```bash
dotnet user-secrets set "Keycloak:ClientSecret" "your-secure-production-secret"
```

The .NET configuration system automatically reads environment variables using the double-underscore (`__`) syntax or the configuration key path.
