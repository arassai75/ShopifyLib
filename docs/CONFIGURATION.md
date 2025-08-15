# Configuration Guide

This guide explains how to configure the Shopify library for your application.

## Configuration Options

The Shopify library supports configuration through multiple sources:

1. **appsettings.json** - Main configuration file
2. **Environment Variables** - For sensitive data
3. **Command Line Arguments** - For runtime overrides
4. **User Secrets** - For development (Visual Studio)

## Basic Configuration

### 1. appsettings.json

Create an `appsettings.json` file in your project:

```json
{
  "Shopify": {
    "ShopDomain": "your-shop.myshopify.com",
    "AccessToken": "your-access-token-here",
    "ApiVersion": "2024-01",
    "MaxRetries": 3,
    "TimeoutSeconds": 30,
    "EnableRateLimiting": true,
    "RequestsPerSecond": 2
  }
}
```

### 2. Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ShopDomain` | string | - | Your Shopify shop domain (e.g., "my-shop.myshopify.com") |
| `AccessToken` | string | - | Your Shopify API access token |
| `ApiVersion` | string | "2024-01" | Shopify API version to use |
| `MaxRetries` | int | 3 | Maximum number of retries for failed requests |
| `TimeoutSeconds` | int | 30 | HTTP request timeout in seconds |
| `EnableRateLimiting` | bool | true | Whether to enable rate limiting |
| `RequestsPerSecond` | int | 2 | Rate limit requests per second |

## Environment Variables

For sensitive data like access tokens, use environment variables:

```bash
# Set environment variables
export Shopify__AccessToken="your-secret-token"
export Shopify__ShopDomain="your-shop.myshopify.com"
```

Or in Windows:
```cmd
set Shopify__AccessToken=your-secret-token
set Shopify__ShopDomain=your-shop.myshopify.com
```

## Using the Configuration

### Method 1: Direct Configuration

```csharp
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Get validated configuration
var shopifyConfig = configuration.GetValidatedShopifyConfiguration();

// Create client
using var client = new ShopifyClient(shopifyConfig);
```

### Method 2: Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ShopifyLib.Models;
using ShopifyLib.Configuration;

// Configure services
var services = new ServiceCollection();
services.AddShopifyServices(configuration);

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get client from DI
using var client = serviceProvider.GetRequiredService<ShopifyClient>();
```

### Method 3: Manual Configuration

```csharp
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Configuration;

var config = new ShopifyConfig
{
    ShopDomain = "your-shop.myshopify.com",
    AccessToken = "your-access-token",
    ApiVersion = "2024-01",
    MaxRetries = 3,
    TimeoutSeconds = 30
};

using var client = new ShopifyClient(config);
```

## Development vs Production

### Development Configuration

Create `appsettings.Development.json` for development settings:

```json
{
  "Shopify": {
    "ShopDomain": "your-dev-shop.myshopify.com",
    "AccessToken": "your-dev-access-token",
    "ApiVersion": "2024-01",
    "MaxRetries": 3,
    "TimeoutSeconds": 30,
    "EnableRateLimiting": true,
    "RequestsPerSecond": 2
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

### Production Configuration

For production, use environment variables or secure configuration management:

```bash
# Production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export Shopify__AccessToken="production-token"
export Shopify__ShopDomain="production-shop.myshopify.com"
```

## Security Best Practices

1. **Never commit access tokens** to source control
2. **Use environment variables** for sensitive data
3. **Use User Secrets** for development (Visual Studio)
4. **Use Azure Key Vault** or similar for production
5. **Rotate access tokens** regularly

### User Secrets (Development)

In Visual Studio, right-click your project → Manage User Secrets:

```json
{
  "Shopify": {
    "AccessToken": "your-secret-token"
  }
}
```

### Azure Key Vault (Production)

```csharp
// Add Azure Key Vault configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddAzureKeyVault(new Uri("https://your-vault.vault.azure.net/"), 
                      new DefaultAzureCredential())
    .Build();
```

## Configuration Validation

The library automatically validates your configuration:

```csharp
// This will throw if configuration is invalid
var config = configuration.GetValidatedShopifyConfiguration();

// Or check manually
if (!config.IsValid())
{
    Console.WriteLine("Invalid configuration!");
    return;
}
```

## Example Console Application

See the `samples/ConsoleApp` for a complete example of configuration usage.

## Troubleshooting

### Common Issues

1. **"Invalid Shopify configuration"**
   - Ensure `ShopDomain` and `AccessToken` are set
   - Check environment variable names (use double underscores)

2. **"Access denied"**
   - Verify your access token is valid
   - Check API permissions for your app

3. **"Rate limit exceeded"**
   - Increase `RequestsPerSecond` or enable rate limiting
   - Implement retry logic with exponential backoff

### Debug Configuration

```csharp
// Print configuration for debugging
var config = configuration.GetShopifyConfiguration();
Console.WriteLine($"Shop Domain: {config.ShopDomain}");
Console.WriteLine($"API Version: {config.ApiVersion}");
Console.WriteLine($"Access Token: {(string.IsNullOrEmpty(config.AccessToken) ? "NOT SET" : "SET")}");
``` 