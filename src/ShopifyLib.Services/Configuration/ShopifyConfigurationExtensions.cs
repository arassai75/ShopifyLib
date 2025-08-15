using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopifyLib.Models;

namespace ShopifyLib.Configuration
{
    /// <summary>
    /// Extension methods for Shopify configuration
    /// </summary>
    public static class ShopifyConfigurationExtensions
    {
        /// <summary>
        /// Binds a configuration section to a ShopifyConfig object
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="sectionName">The section name (default: "Shopify")</param>
        /// <returns>The bound ShopifyConfig</returns>
        public static ShopifyConfig GetShopifyConfig(this IConfiguration configuration, string sectionName = "Shopify")
        {
            var section = configuration.GetSection(sectionName);
            var config = new ShopifyConfig();
            
            // Manual binding for .NET Framework 4.8 compatibility
            config.ShopDomain = section["ShopDomain"] ?? "";
            config.AccessToken = section["AccessToken"] ?? "";
            config.ApiVersion = section["ApiVersion"] ?? "2024-01";
            
            if (int.TryParse(section["MaxRetries"], out int maxRetries))
                config.MaxRetries = maxRetries;
                
            if (int.TryParse(section["TimeoutSeconds"], out int timeoutSeconds))
                config.TimeoutSeconds = timeoutSeconds;
                
            if (bool.TryParse(section["EnableRateLimiting"], out bool enableRateLimiting))
                config.EnableRateLimiting = enableRateLimiting;
                
            if (int.TryParse(section["RequestsPerSecond"], out int requestsPerSecond))
                config.RequestsPerSecond = requestsPerSecond;

            if (!config.IsValid())
            {
                throw new InvalidOperationException(string.Format("Invalid Shopify configuration in section '{0}'. ShopDomain and AccessToken are required.", sectionName));
            }

            return config;
        }

        /// <summary>
        /// Binds a configuration section to a ShopifyConfig object with validation
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="sectionName">The section name (default: "Shopify")</param>
        /// <param name="validate">Whether to validate the configuration</param>
        /// <returns>The bound ShopifyConfig</returns>
        public static ShopifyConfig GetShopifyConfig(this IConfiguration configuration, string sectionName, bool validate)
        {
            var section = configuration.GetSection(sectionName);
            var config = new ShopifyConfig();
            
            // Manual binding for .NET Framework 4.8 compatibility
            config.ShopDomain = section["ShopDomain"] ?? "";
            config.AccessToken = section["AccessToken"] ?? "";
            config.ApiVersion = section["ApiVersion"] ?? "2024-01";
            
            if (int.TryParse(section["MaxRetries"], out int maxRetries))
                config.MaxRetries = maxRetries;
                
            if (int.TryParse(section["TimeoutSeconds"], out int timeoutSeconds))
                config.TimeoutSeconds = timeoutSeconds;
                
            if (bool.TryParse(section["EnableRateLimiting"], out bool enableRateLimiting))
                config.EnableRateLimiting = enableRateLimiting;
                
            if (int.TryParse(section["RequestsPerSecond"], out int requestsPerSecond))
                config.RequestsPerSecond = requestsPerSecond;

            if (validate && !config.IsValid())
            {
                throw new InvalidOperationException(string.Format("Invalid Shopify configuration in section '{0}'. ShopDomain and AccessToken are required.", sectionName));
            }

            return config;
        }

        /// <summary>
        /// Binds a configuration section to a ShopifyConfig object with custom validation
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="sectionName">The section name (default: "Shopify")</param>
        /// <param name="validator">Custom validation function</param>
        /// <returns>The bound ShopifyConfig</returns>
        public static ShopifyConfig GetShopifyConfig(this IConfiguration configuration, string sectionName, Func<ShopifyConfig, bool> validator)
        {
            var section = configuration.GetSection(sectionName);
            var config = new ShopifyConfig();
            
            // Manual binding for .NET Framework 4.8 compatibility
            config.ShopDomain = section["ShopDomain"] ?? "";
            config.AccessToken = section["AccessToken"] ?? "";
            config.ApiVersion = section["ApiVersion"] ?? "2024-01";
            
            if (int.TryParse(section["MaxRetries"], out int maxRetries))
                config.MaxRetries = maxRetries;
                
            if (int.TryParse(section["TimeoutSeconds"], out int timeoutSeconds))
                config.TimeoutSeconds = timeoutSeconds;
                
            if (bool.TryParse(section["EnableRateLimiting"], out bool enableRateLimiting))
                config.EnableRateLimiting = enableRateLimiting;
                
            if (int.TryParse(section["RequestsPerSecond"], out int requestsPerSecond))
                config.RequestsPerSecond = requestsPerSecond;

            if (validator != null && !validator(config))
            {
                throw new InvalidOperationException(string.Format("Invalid Shopify configuration in section '{0}' according to custom validator.", sectionName));
            }

            return config;
        }

        /// <summary>
        /// Validates Shopify configuration and throws if invalid
        /// </summary>
        /// <param name="configuration">The configuration root</param>
        /// <returns>The validated Shopify configuration</returns>
        public static ShopifyConfig GetValidatedShopifyConfiguration(this IConfiguration configuration)
        {
            var shopifyConfig = configuration.GetShopifyConfig(); // Changed from GetShopifyConfiguration
            
            if (!shopifyConfig.IsValid())
            {
                throw new InvalidOperationException(
                    "Invalid Shopify configuration. Please ensure the following are set in your configuration:\n" +
                    "- Shopify:ShopDomain (e.g., 'your-shop.myshopify.com')\n" +
                    "- Shopify:AccessToken (your Shopify API access token)");
            }

            return shopifyConfig;
        }
    }
} 