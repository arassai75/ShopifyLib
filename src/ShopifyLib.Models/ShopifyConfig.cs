using System;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Configuration settings for Shopify API integration
    /// </summary>
    public class ShopifyConfig
    {
        /// <summary>
        /// The Shopify shop domain (e.g., "my-shop.myshopify.com")
        /// </summary>
        public string ShopDomain { get; set; } = "";

        /// <summary>
        /// The access token for API authentication
        /// </summary>
        public string AccessToken { get; set; } = "";

        /// <summary>
        /// The API version to use (default: "2024-01")
        /// </summary>
        public string ApiVersion { get; set; } = "2024-01";

        /// <summary>
        /// Maximum number of retry attempts for failed requests (default: 3)
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Request timeout in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to enable rate limiting (default: true)
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;

        /// <summary>
        /// Number of requests allowed per second (default: 2)
        /// </summary>
        public int RequestsPerSecond { get; set; } = 2;

        /// <summary>
        /// Validates that the configuration has the required fields
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ShopDomain) && !string.IsNullOrWhiteSpace(AccessToken);
        }
    }
} 