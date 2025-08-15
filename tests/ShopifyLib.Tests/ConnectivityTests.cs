using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class ConnectivityTests : IDisposable
    {
        private readonly ShopifyClient _client;

        public ConnectivityTests()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var shopifyConfig = configuration.GetShopifyConfig();
            
            if (!shopifyConfig.IsValid())
            {
                throw new InvalidOperationException("Shopify configuration is not valid. Please check your appsettings.json or environment variables.");
            }

            _client = new ShopifyClient(shopifyConfig);
        }

        [Fact]
        public async Task CanConnectToShopifyAPI()
        {
            // Act - Try to get product count (lightweight operation)
            var count = await _client.Products.GetCountAsync();

            // Assert - If we get here without exception, connection works
            Assert.True(count >= 0, "Should be able to connect to Shopify API");
        }

        [Fact]
        public async Task CanRetrieveBasicProductData()
        {
            // Act - Try to get a small list of products
            var products = await _client.Products.GetAllAsync(limit: 1);

            // Assert - Should be able to retrieve data
            Assert.NotNull(products);
        }

        [Fact]
        public async Task ConfigurationIsValid()
        {
            // This test verifies that the configuration loaded correctly
            // and the client was created successfully
            
            // Act & Assert - If we get here, configuration is valid
            Assert.NotNull(_client);
            Assert.NotNull(_client.Products);
            Assert.NotNull(_client.Metafields);
            Assert.NotNull(_client.Images);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 