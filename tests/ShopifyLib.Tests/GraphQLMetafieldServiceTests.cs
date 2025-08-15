using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using ShopifyLib.Services;

namespace ShopifyLib.Tests
{
    public class GraphQLMetafieldServiceTests
    {
        private readonly Mock<IGraphQLService> _mockGraphQLService;
        private readonly GraphQLMetafieldService _service;

        public GraphQLMetafieldServiceTests()
        {
            _mockGraphQLService = new Mock<IGraphQLService>();
            _service = new GraphQLMetafieldService(_mockGraphQLService.Object);
        }

        [Fact]
        public void Constructor_WithNullGraphQLService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GraphQLMetafieldService(null));
        }

        [Fact]
        public async Task GetMetafieldsAsync_WithValidOwnerGid_ReturnsMetafields()
        {
            // Arrange
            var ownerGid = "gid://shopify/Product/123456789";
            var expectedResponse = @"{
                ""data"": {
                    ""node"": {
                        ""metafields"": {
                            ""edges"": [
                                {
                                    ""node"": {
                                        ""id"": ""gid://shopify/Metafield/123"",
                                        ""namespace"": ""custom"",
                                        ""key"": ""color"",
                                        ""value"": ""blue"",
                                        ""type"": ""single_line_text_field""
                                    }
                                }
                            ]
                        }
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetMetafieldsAsync(ownerGid);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("gid://shopify/Metafield/123", result[0].Id);
            Assert.Equal("custom", result[0].Namespace);
            Assert.Equal("color", result[0].Key);
            Assert.Equal("blue", result[0].Value);
            Assert.Equal("single_line_text_field", result[0].Type);

            _mockGraphQLService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMetafieldsAsync_WithEmptyOwnerGid_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMetafieldsAsync(""));
        }

        [Fact]
        public async Task GetMetafieldsAsync_WithNullOwnerGid_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMetafieldsAsync(null));
        }

        [Fact]
        public async Task CreateOrUpdateMetafieldAsync_WithValidInput_ReturnsMetafield()
        {
            // Arrange
            var metafieldInput = new MetafieldInput
            {
                OwnerId = "gid://shopify/Product/123456789",
                Namespace = "custom",
                Key = "size",
                Value = "large",
                Type = "single_line_text_field"
            };

            var expectedResponse = @"{
                ""data"": {
                    ""metafieldsSet"": {
                        ""metafields"": [
                            {
                                ""id"": ""gid://shopify/Metafield/456"",
                                ""namespace"": ""custom"",
                                ""key"": ""size"",
                                ""value"": ""large"",
                                ""type"": ""single_line_text_field""
                            }
                        ],
                        ""userErrors"": []
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.CreateOrUpdateMetafieldAsync(metafieldInput);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("gid://shopify/Metafield/456", result.Id);
            Assert.Equal("custom", result.Namespace);
            Assert.Equal("size", result.Key);
            Assert.Equal("large", result.Value);
            Assert.Equal("single_line_text_field", result.Type);

            _mockGraphQLService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateMetafieldAsync_WithNullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateOrUpdateMetafieldAsync(null));
        }

        [Fact]
        public async Task CreateOrUpdateMetafieldAsync_WithUserErrors_ThrowsGraphQLUserException()
        {
            // Arrange
            var metafieldInput = new MetafieldInput
            {
                OwnerId = "gid://shopify/Product/123456789",
                Namespace = "custom",
                Key = "size",
                Value = "large",
                Type = "single_line_text_field"
            };

            var expectedResponse = @"{
                ""data"": {
                    ""metafieldsSet"": {
                        ""metafields"": [],
                        ""userErrors"": [
                            {
                                ""field"": [""value""],
                                ""message"": ""Invalid value""
                            }
                        ]
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<GraphQLUserException>(
                () => _service.CreateOrUpdateMetafieldAsync(metafieldInput));
            Assert.Contains("Failed to create/update metafield", exception.Message);
            Assert.Contains("Invalid value", exception.Message);
        }

        [Fact]
        public async Task DeleteMetafieldAsync_WithValidGid_ReturnsTrue()
        {
            // Arrange
            var metafieldGid = "gid://shopify/Metafield/123";
            var expectedResponse = @"{
                ""data"": {
                    ""metafieldDelete"": {
                        ""deletedId"": ""gid://shopify/Metafield/123"",
                        ""userErrors"": []
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.DeleteMetafieldAsync(metafieldGid);

            // Assert
            Assert.True(result);
            _mockGraphQLService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMetafieldAsync_WithEmptyGid_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteMetafieldAsync(""));
        }

        [Fact]
        public async Task DeleteMetafieldAsync_WithNullGid_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteMetafieldAsync(null));
        }

        [Fact]
        public async Task DeleteMetafieldAsync_WithUserErrors_ThrowsGraphQLUserException()
        {
            // Arrange
            var metafieldGid = "gid://shopify/Metafield/123";
            var expectedResponse = @"{
                ""data"": {
                    ""metafieldDelete"": {
                        ""deletedId"": null,
                        ""userErrors"": [
                            {
                                ""field"": [""id""],
                                ""message"": ""Metafield not found""
                            }
                        ]
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<GraphQLUserException>(
                () => _service.DeleteMetafieldAsync(metafieldGid));
            Assert.Contains("Failed to delete metafield", exception.Message);
            Assert.Contains("Metafield not found", exception.Message);
        }

        [Fact]
        public async Task GetMetafieldDefinitionsAsync_WithValidOwnerType_ReturnsDefinitions()
        {
            // Arrange
            var expectedResponse = @"{
                ""data"": {
                    ""metafieldDefinitions"": {
                        ""edges"": [
                            {
                                ""node"": {
                                    ""id"": ""gid://shopify/MetafieldDefinition/123"",
                                    ""name"": ""Color"",
                                    ""namespace"": ""custom"",
                                    ""key"": ""color"",
                                    ""type"": ""single_line_text_field""
                                }
                            }
                        ]
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetMetafieldDefinitionsAsync("PRODUCT");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("gid://shopify/MetafieldDefinition/123", result[0].Id);
            Assert.Equal("Color", result[0].Name);
            Assert.Equal("custom", result[0].Namespace);
            Assert.Equal("color", result[0].Key);
            Assert.Equal("single_line_text_field", result[0].Type);

            _mockGraphQLService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMetafieldDefinitionsAsync_WithEmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            var expectedResponse = @"{
                ""data"": {
                    ""metafieldDefinitions"": {
                        ""edges"": []
                    }
                }
            }";

            _mockGraphQLService
                .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetMetafieldDefinitionsAsync("PRODUCT");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
} 