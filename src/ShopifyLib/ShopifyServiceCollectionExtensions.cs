using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using ShopifyLib.Services;

namespace ShopifyLib
{
    /// <summary>
    /// Extension methods for configuring Shopify services in dependency injection.
    /// </summary>
    public static class ShopifyServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Shopify services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddShopifyServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind Shopify configuration manually for .NET Framework 4.8 compatibility
            var shopifyConfig = configuration.GetShopifyConfig();
            
            // Register configuration as singleton
            services.AddSingleton(shopifyConfig);

            // Register services
            services.AddScoped<IGraphQLService, GraphQLService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IMetafieldService, MetafieldService>();
            services.AddScoped<IGraphQLMetafieldService, GraphQLMetafieldService>();
            services.AddScoped<IFileMetafieldService, FileMetafieldService>();
            services.AddScoped<IImageTransformationService, ImageTransformationService>();
            services.AddScoped<EnhancedFileServiceWithMetadata>();

            // Register ShopifyClient
            services.AddScoped<ShopifyClient>();

            return services;
        }

        /// <summary>
        /// Adds Shopify services to the service collection with custom configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="shopifyConfig">The Shopify configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddShopifyServices(this IServiceCollection services, ShopifyConfig shopifyConfig)
        {
            // Validate configuration
            if (!shopifyConfig.IsValid())
            {
                throw new InvalidOperationException("Invalid Shopify configuration. ShopDomain and AccessToken are required.");
            }

            // Register configuration as singleton
            services.AddSingleton(shopifyConfig);

            // Register Shopify client as scoped
            services.AddScoped<ShopifyClient>();

            return services;
        }
    }
} 