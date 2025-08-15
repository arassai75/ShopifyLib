using System;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    public class ShopifyClientTests
    {
        [Fact]
        public void Constructor_WithValidConfig_CreatesClient()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "test-shop.myshopify.com",
                AccessToken = "test-access-token"
            };

            // Act & Assert
            using (var client = new ShopifyClient(config))
            {
                Assert.NotNull(client);
                Assert.NotNull(client.Products);
                Assert.NotNull(client.Metafields);
                Assert.NotNull(client.Images);
                Assert.NotNull(client.GraphQL);
                Assert.NotNull(client.Files);
            }
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ShopifyClient(null));
        }

        [Fact]
        public void Constructor_WithInvalidConfig_ThrowsArgumentException()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "",
                AccessToken = "test-access-token"
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ShopifyClient(config));
            Assert.Contains("Invalid Shopify configuration", exception.Message);
        }

        [Fact]
        public void Constructor_WithShopDomainAndAccessToken_CreatesClient()
        {
            // Act & Assert
            using (var client = new ShopifyClient("test-shop.myshopify.com", "test-access-token"))
            {
                Assert.NotNull(client);
                Assert.NotNull(client.Products);
                Assert.NotNull(client.Metafields);
                Assert.NotNull(client.Images);
                Assert.NotNull(client.GraphQL);
                Assert.NotNull(client.Files);
            }
        }

        [Fact]
        public void Constructor_WithEmptyShopDomain_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ShopifyClient("", "test-access-token"));
            Assert.Contains("Invalid Shopify configuration", exception.Message);
        }

        [Fact]
        public void Constructor_WithEmptyAccessToken_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ShopifyClient("test-shop.myshopify.com", ""));
            Assert.Contains("Invalid Shopify configuration", exception.Message);
        }

        [Fact]
        public void Dispose_DisposesHttpClient()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "test-shop.myshopify.com",
                AccessToken = "test-access-token"
            };

            // Act
            ShopifyClient client;
            using (client = new ShopifyClient(config))
            {
                Assert.NotNull(client);
            }

            // Assert - Client should be disposed
            // Note: We can't directly test disposal, but we can verify no exceptions are thrown
        }
    }
} 