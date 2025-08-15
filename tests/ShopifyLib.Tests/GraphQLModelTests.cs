using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    public class GraphQLModelTests
    {
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
            var json = @"{
                ""files"": [
                    {
                        ""id"": ""gid://shopify/MediaImage/123"",
                        ""fileStatus"": ""READY"",
                        ""alt"": ""Test image"",
                        ""createdAt"": ""2024-01-01T00:00:00Z"",
                        ""image"": {
                            ""width"": 100,
                            ""height"": 100
                        }
                    }
                ],
                ""userErrors"": []
            }";

            // Act
            var response = JsonConvert.DeserializeObject<FileCreateResponse>(json);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Files);
            Assert.Single(response.Files);
            Assert.Empty(response.UserErrors);

            var file = response.Files[0];
            Assert.Equal("gid://shopify/MediaImage/123", file.Id);
            Assert.Equal("READY", file.FileStatus);
            Assert.Equal("Test image", file.Alt);
            Assert.Equal("2024-01-01T00:00:00Z", file.CreatedAt);
            Assert.NotNull(file.Image);
            Assert.Equal(100, file.Image.Width);
            Assert.Equal(100, file.Image.Height);
        }

        [Fact]
        public void FileCreateResponse_WithUserErrors_DeserializesCorrectly()
        {
            // Arrange
            var json = @"{
                ""files"": [],
                ""userErrors"": [
                    {
                        ""field"": [""file"", ""content""],
                        ""message"": ""Invalid file content""
                    }
                ]
            }";

            // Act
            var response = JsonConvert.DeserializeObject<FileCreateResponse>(json);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Files);
            Assert.Single(response.UserErrors);

            var error = response.UserErrors[0];
            Assert.Equal(2, error.Field.Count);
            Assert.Equal("file", error.Field[0]);
            Assert.Equal("content", error.Field[1]);
            Assert.Equal("Invalid file content", error.Message);
        }

        [Fact]
        public void FileCreateInput_AllProperties_SetCorrectly()
        {
            // Arrange & Act
            var fileInput = new FileCreateInput
            {
                OriginalSource = "test-content",
                ContentType = FileContentType.File,
                Alt = "Test file"
            };

            // Assert
            Assert.Equal("test-content", fileInput.OriginalSource);
            Assert.Equal(FileContentType.File, fileInput.ContentType);
            Assert.Equal("Test file", fileInput.Alt);
        }

        [Fact]
        public void ImageInfo_Properties_SetCorrectly()
        {
            // Arrange & Act
            var imageInfo = new ImageInfo
            {
                Width = 800,
                Height = 600
            };

            // Assert
            Assert.Equal(800, imageInfo.Width);
            Assert.Equal(600, imageInfo.Height);
        }

        [Fact]
        public void UserError_Properties_SetCorrectly()
        {
            // Arrange & Act
            var userError = new UserError
            {
                Field = new List<string> { "file", "content" },
                Message = "Invalid content type"
            };

            // Assert
            Assert.Equal(2, userError.Field.Count);
            Assert.Equal("file", userError.Field[0]);
            Assert.Equal("content", userError.Field[1]);
            Assert.Equal("Invalid content type", userError.Message);
        }
    }
} 