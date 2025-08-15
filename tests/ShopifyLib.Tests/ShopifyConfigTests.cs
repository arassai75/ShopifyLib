using System;
using Xunit;
using ShopifyLib.Configuration;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    public class ShopifyConfigTests
    {
        [Fact]
        public void IsValid_WithValidConfig_ReturnsTrue()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "test-shop.myshopify.com",
                AccessToken = "test-access-token"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValid_WithEmptyShopDomain_ReturnsFalse()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "",
                AccessToken = "test-access-token"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValid_WithNullShopDomain_ReturnsFalse()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = null,
                AccessToken = "test-access-token"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValid_WithEmptyAccessToken_ReturnsFalse()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "test-shop.myshopify.com",
                AccessToken = ""
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValid_WithNullAccessToken_ReturnsFalse()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "test-shop.myshopify.com",
                AccessToken = null
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void Constructor_WithDefaultValues_SetsCorrectDefaults()
        {
            // Act
            var config = new ShopifyConfig();

            // Assert
            Assert.Equal("2024-01", config.ApiVersion);
            Assert.Equal(3, config.MaxRetries);
            Assert.Equal(30, config.TimeoutSeconds);
            Assert.True(config.EnableRateLimiting);
            Assert.Equal(2, config.RequestsPerSecond);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var config = new ShopifyConfig
            {
                ShopDomain = "custom-shop.myshopify.com",
                AccessToken = "custom-token",
                ApiVersion = "2023-10",
                MaxRetries = 5,
                TimeoutSeconds = 60,
                EnableRateLimiting = false,
                RequestsPerSecond = 5
            };

            // Assert
            Assert.Equal("custom-shop.myshopify.com", config.ShopDomain);
            Assert.Equal("custom-token", config.AccessToken);
            Assert.Equal("2023-10", config.ApiVersion);
            Assert.Equal(5, config.MaxRetries);
            Assert.Equal(60, config.TimeoutSeconds);
            Assert.False(config.EnableRateLimiting);
            Assert.Equal(5, config.RequestsPerSecond);
        }
    }
} 