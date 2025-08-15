using System;
using System.Collections.Generic;
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
    public class ImageUploadGraphQLTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public ImageUploadGraphQLTest()
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
        public async Task UploadIndigoImageToShopify_DisplayAllResponseDetails()
        {
            // Arrange - Use the specified Indigo image URL
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - Test Upload";
            
            Console.WriteLine("=== Starting Image Upload Test ===");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Act - Upload image using GraphQL fileCreate mutation
                Console.WriteLine("🔄 Uploading image to Shopify using GraphQL...");
                
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                // Assert and Display Results
                Console.WriteLine("✅ Image upload completed successfully!");
                Console.WriteLine();
                
                // Display the complete response details
                Console.WriteLine("=== COMPLETE SHOPIFY RESPONSE DETAILS ===");
                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
                Console.WriteLine();

                // Validate response structure
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.NotEmpty(response.Files);
                Assert.Empty(response.UserErrors);

                var uploadedFile = response.Files[0];
                
                // Display detailed file information
                Console.WriteLine("=== UPLOADED FILE DETAILS ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");
                
                // Display image-specific details if available
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== IMAGE DIMENSIONS ===");
                    Console.WriteLine($"📏 Width: {uploadedFile.Image.Width} pixels");
                    Console.WriteLine($"📐 Height: {uploadedFile.Image.Height} pixels");
                    Console.WriteLine($"📊 Aspect Ratio: {(double)uploadedFile.Image.Width / uploadedFile.Image.Height:F2}");
                    
                    // Validate image dimensions
                    Assert.True(uploadedFile.Image.Width > 0, "Image width should be greater than 0");
                    Assert.True(uploadedFile.Image.Height > 0, "Image height should be greater than 0");

                    Console.WriteLine();
                    Console.WriteLine("=== SHOPIFY CDN URLS ===");
                    Console.WriteLine($"🌐 Shopify CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"🔗 Original Source: {uploadedFile.Image.OriginalSrc ?? "Not available"}");
                    Console.WriteLine($"🔄 Transformed Source: {uploadedFile.Image.TransformedSrc ?? "Not available"}");
                    Console.WriteLine($"📷 Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                    
                    // Validate that we have at least one URL
                    var hasUrl = !string.IsNullOrEmpty(uploadedFile.Image.Url) || 
                                !string.IsNullOrEmpty(uploadedFile.Image.Src) || 
                                !string.IsNullOrEmpty(uploadedFile.Image.OriginalSrc);
                    Assert.True(hasUrl, "At least one image URL should be available");
                }
                else
                {
                    Console.WriteLine("⚠️  Image dimensions not available in response");
                }

                // Display file status information
                Console.WriteLine();
                Console.WriteLine("=== FILE STATUS INFORMATION ===");
                Console.WriteLine($"🔄 Processing Status: {uploadedFile.FileStatus}");
                
                // Check if file is ready for use
                if (uploadedFile.FileStatus.Equals("READY", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✅ File is ready for use!");
                }
                else if (uploadedFile.FileStatus.Equals("UPLOADED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("⏳ File uploaded successfully, processing in progress...");
                }
                else
                {
                    Console.WriteLine($"ℹ️  File status: {uploadedFile.FileStatus}");
                }

                // Display GraphQL ID information
                Console.WriteLine();
                Console.WriteLine("=== GRAPHQL ID INFORMATION ===");
                Console.WriteLine($"🆔 Full GraphQL ID: {uploadedFile.Id}");
                
                if (uploadedFile.Id.StartsWith("gid://shopify/MediaImage/"))
                {
                    var idParts = uploadedFile.Id.Split('/');
                    if (idParts.Length >= 4)
                    {
                        Console.WriteLine($"🏷️  Resource Type: MediaImage");
                        Console.WriteLine($"🔢 Numeric ID: {idParts[3]}");
                    }
                }

                // Display any additional metadata
                Console.WriteLine();
                Console.WriteLine("=== ADDITIONAL METADATA ===");
                Console.WriteLine($"📋 Response contains {response.Files.Count} file(s)");
                Console.WriteLine($"❌ User Errors: {response.UserErrors.Count}");
                
                if (response.UserErrors.Count > 0)
                {
                    Console.WriteLine("⚠️  User errors found:");
                    foreach (var error in response.UserErrors)
                    {
                        Console.WriteLine($"   - Field: {string.Join(", ", error.Field)}");
                        Console.WriteLine($"   - Message: {error.Message}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("=== TEST SUMMARY ===");
                Console.WriteLine($"✅ Successfully uploaded image to Shopify");
                Console.WriteLine($"✅ File ID: {uploadedFile.Id}");
                Console.WriteLine($"✅ Status: {uploadedFile.FileStatus}");
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"✅ Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"✅ Shopify CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"✅ Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                }
                Console.WriteLine("✅ Image uploaded without attaching to any product or variant");
                Console.WriteLine("✅ All response details displayed above");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 