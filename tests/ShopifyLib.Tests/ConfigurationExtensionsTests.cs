using System;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib.Configuration;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    public class ConfigurationExtensionsTests
    {
        [Fact]
        public void GetShopifyConfig_WithValidConfiguration_ReturnsValidConfig()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token"),
                    new KeyValuePair<string, string>("Shopify:ApiVersion", "2024-01"),
                    new KeyValuePair<string, string>("Shopify:MaxRetries", "3"),
                    new KeyValuePair<string, string>("Shopify:TimeoutSeconds", "30"),
                    new KeyValuePair<string, string>("Shopify:EnableRateLimiting", "true"),
                    new KeyValuePair<string, string>("Shopify:RequestsPerSecond", "2")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("test-shop.myshopify.com", config.ShopDomain);
            Assert.Equal("test-access-token", config.AccessToken);
            Assert.Equal("2024-01", config.ApiVersion);
            Assert.Equal(3, config.MaxRetries);
            Assert.Equal(30, config.TimeoutSeconds);
            Assert.True(config.EnableRateLimiting);
            Assert.Equal(2, config.RequestsPerSecond);
            Assert.True(config.IsValid());
        }

        [Fact]
        public void GetShopifyConfig_WithCustomSectionName_ReturnsValidConfig()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("CustomShopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("CustomShopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig("CustomShopify");

            // Assert
            Assert.NotNull(config);
            Assert.Equal("test-shop.myshopify.com", config.ShopDomain);
            Assert.Equal("test-access-token", config.AccessToken);
            Assert.True(config.IsValid());
        }

        [Fact]
        public void GetShopifyConfig_WithMissingRequiredFields_ThrowsInvalidOperationException()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", ""),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetShopifyConfig());
            Assert.Contains("Invalid Shopify configuration", exception.Message);
        }

        [Fact]
        public void GetShopifyConfig_WithValidationDisabled_ReturnsConfigEvenIfInvalid()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", ""),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig("Shopify", false);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("", config.ShopDomain);
            Assert.Equal("test-access-token", config.AccessToken);
            Assert.False(config.IsValid());
        }

        [Fact]
        public void GetShopifyConfig_WithCustomValidator_ValidatesCorrectly()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig("Shopify", config => config.ShopDomain.Contains("test"));

            // Assert
            Assert.NotNull(config);
            Assert.True(config.IsValid());
        }

        [Fact]
        public void GetShopifyConfig_WithCustomValidatorThatFails_ThrowsException()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                configuration.GetShopifyConfig("Shopify", config => config.ShopDomain.Contains("invalid")));
            Assert.Contains("Invalid Shopify configuration", exception.Message);
        }

        [Fact]
        public void GetShopifyConfig_WithNullValidator_DoesNotValidate()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig("Shopify", (Func<ShopifyConfig, bool>)null);

            // Assert
            Assert.NotNull(config);
            Assert.True(config.IsValid());
        }

        [Fact]
        public void GetShopifyConfig_WithDefaultValues_UsesDefaults()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("2024-01", config.ApiVersion);
            Assert.Equal(3, config.MaxRetries);
            Assert.Equal(30, config.TimeoutSeconds);
            Assert.True(config.EnableRateLimiting);
            Assert.Equal(2, config.RequestsPerSecond);
        }

        [Fact]
        public void GetShopifyConfig_WithCustomValues_OverridesDefaults()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Shopify:ShopDomain", "test-shop.myshopify.com"),
                    new KeyValuePair<string, string>("Shopify:AccessToken", "test-access-token"),
                    new KeyValuePair<string, string>("Shopify:ApiVersion", "2023-10"),
                    new KeyValuePair<string, string>("Shopify:MaxRetries", "5"),
                    new KeyValuePair<string, string>("Shopify:TimeoutSeconds", "60"),
                    new KeyValuePair<string, string>("Shopify:EnableRateLimiting", "false"),
                    new KeyValuePair<string, string>("Shopify:RequestsPerSecond", "5")
                })
                .Build();

            // Act
            var config = configuration.GetShopifyConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("2023-10", config.ApiVersion);
            Assert.Equal(5, config.MaxRetries);
            Assert.Equal(60, config.TimeoutSeconds);
            Assert.False(config.EnableRateLimiting);
            Assert.Equal(5, config.RequestsPerSecond);
        }
    }
} 