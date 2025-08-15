using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Newtonsoft.Json;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class GraphQLTests : IDisposable
    {
        private readonly ShopifyClient _client;

        public GraphQLTests()
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
        public async Task CanExecuteGraphQLQuery()
        {
            // Arrange
            const string query = @"
                query {
                    shop {
                        name
                        myshopifyDomain
                    }
                }";

            // Act
            var response = await _client.GraphQL.ExecuteQueryAsync(query);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("shop", response);
        }

        [Fact]
        public async Task CanCreateFileViaGraphQL()
        {
            // Arrange - Create a simple test image (1x1 pixel PNG)
            var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
            var fileName = "test-image.png";
            var contentType = "image/png";

            using var stream = new MemoryStream(pngBytes);

            try
            {
                // Act
                var response = await _client.Files.UploadFileAsync(stream, fileName, contentType, "Test image");

                // Assert
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.NotEmpty(response.Files);
                Assert.Empty(response.UserErrors);

                var file = response.Files[0];
                Assert.NotNull(file.Id);
                Assert.NotNull(file.FileStatus);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to create files via GraphQL"))
            {
                // Fail test with clear message if fileCreate is not available
                Assert.Fail("GraphQL fileCreate mutation Error:" + ex);
            }
        }

        [Fact]
        public async Task CanCreateMultipleFilesViaGraphQL()
        {
            // Arrange - Use a real, public file URL
            var files = new List<FileCreateInput>
            {
                new FileCreateInput
                {
                    OriginalSource = "https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png",
                    ContentType = FileContentType.Image
                }
            };

            try
            {
                // Act
                var response = await _client.Files.UploadFilesAsync(files);

                // Assert
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.NotEmpty(response.Files);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to create files via GraphQL"))
            {
                // Fail test with clear message if fileCreate is not available
                Assert.Fail("GraphQL fileCreate mutation Error:" + ex);
            }
        }

        [Fact]
        public void FileCreateInput_Serialization_WorksCorrectly()
        {
            // Arrange
            var input = new FileCreateInput
            {
                OriginalSource = "base64content",
                ContentType = FileContentType.Image,
                Alt = "Test image"
            };

            // Act
            var json = JsonConvert.SerializeObject(input, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });

            // Assert
            Assert.Contains("base64content", json);
            Assert.Contains("IMAGE", json);
            Assert.Contains("Test image", json);
        }

        [Fact]
        public void FileCreateResponse_Deserialization_WorksCorrectly()
        {
            // Arrange
            var json = @"{""files"": [{""id"": ""gid://shopify/MediaImage/123"", ""fileStatus"": ""READY"", ""alt"": ""Test image"", ""createdAt"": ""2024-01-01T00:00:00Z"", ""image"": {""width"": 300, ""height"": 200}}], ""userErrors"": []}";;

            // Act
            var response = JsonConvert.DeserializeObject<FileCreateResponse>(json);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Files);
            Assert.Single(response.Files);
            var file = response.Files[0];
            Assert.Equal("gid://shopify/MediaImage/123", file.Id);
            Assert.Equal("READY", file.FileStatus);
            Assert.Equal("Test image", file.Alt);
            Assert.Equal("2024-01-01T00:00:00Z", file.CreatedAt);
            Assert.NotNull(file.Image);
            Assert.Equal(300, file.Image.Width);
            Assert.Equal(200, file.Image.Height);
        }

        [Fact]
        public void FileCreateResponse_Deserialization_WithUserErrors_WorksCorrectly()
        {
            // Arrange
            var json = @"{""files"": [], ""userErrors"": [{""field"": [""originalSource""], ""message"": ""URL not accessible""}]}";

            // Act
            var response = JsonConvert.DeserializeObject<FileCreateResponse>(json);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Files);
            Assert.Single(response.UserErrors);
            var error = response.UserErrors[0];
            Assert.Contains("originalSource", error.Field);
            Assert.Equal("URL not accessible", error.Message);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 