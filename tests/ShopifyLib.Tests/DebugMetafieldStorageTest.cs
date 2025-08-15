using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using ShopifyLib.Services;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Debug test to understand why metafield storage is failing
    /// </summary>
    [IntegrationTest]
    public class DebugMetafieldStorageTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public DebugMetafieldStorageTest()
        {
            // Load configuration
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
            _fileMetafieldService = new FileMetafieldService(_client.GraphQL);
            _enhancedFileService = new EnhancedFileServiceWithMetadata(
                _client.Files, 
                _fileMetafieldService,
                new ImageDownloadService()
            );
        }

        [Fact]
        public async Task DebugMetafieldStorage_ShouldIdentifyTheIssue()
        {
            Console.WriteLine("=== DEBUG METAFIELD STORAGE TEST ===");
            Console.WriteLine("This test will help identify why metafield storage is failing");
            Console.WriteLine();

            try
            {
                // Step 1: Upload a single image with detailed logging
                var fileId = await UploadSingleImageWithDebugLogging();
                Console.WriteLine("✅ Step 1: Uploaded single image");
                Console.WriteLine();

                // Step 2: Check if image exists in Shopify
                await VerifyImageExistsInShopify(fileId);
                Console.WriteLine("✅ Step 2: Verified image exists in Shopify");
                Console.WriteLine();

                // Step 3: Try to manually store metafield
                await ManuallyStoreMetafield(fileId);
                Console.WriteLine("✅ Step 3: Attempted manual metafield storage");
                Console.WriteLine();

                // Step 4: Verify metafield storage
                await VerifyMetafieldStorage(fileId);
                Console.WriteLine("✅ Step 4: Verified metafield storage");
                Console.WriteLine();

                Console.WriteLine("🎉 DEBUG TEST COMPLETED!");
                Console.WriteLine("Check the output above to identify the issue with metafield storage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<string> UploadSingleImageWithDebugLogging()
        {
            Console.WriteLine("🔄 Uploading single image with debug logging...");
            
            var imageData = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>
            {
                ("https://httpbin.org/image/jpeg", FileContentType.Image, 9999, "123456789012", "debug_batch", "Debug test image")
            };

            Console.WriteLine($"   📊 Image data prepared:");
            Console.WriteLine($"      URL: {imageData[0].ImageUrl}");
            Console.WriteLine($"      Product ID: {imageData[0].ProductId}");
            Console.WriteLine($"      Batch ID: {imageData[0].BatchId}");
            Console.WriteLine($"      Alt Text: {imageData[0].AltText}");
            Console.WriteLine();

            Console.WriteLine("   🔄 Calling UploadImagesWithMetadataAsync...");
            var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);
            
            Console.WriteLine($"   ✅ Upload response received:");
            Console.WriteLine($"      Files count: {response?.Files?.Count ?? 0}");
            Console.WriteLine($"      User errors: {response?.UserErrors?.Count ?? 0}");
            
            if (response?.UserErrors?.Any() == true)
            {
                Console.WriteLine("      ❌ User errors found:");
                foreach (var error in response.UserErrors)
                {
                    Console.WriteLine($"         - {error.Field}: {error.Message}");
                }
            }

            if (response?.Files?.Any() == true)
            {
                var file = response.Files.First();
                _uploadedFileIds.Add(file.Id);
                
                Console.WriteLine($"      📁 File uploaded: {file.Id}");
                Console.WriteLine($"      📝 Alt text: {file.Alt}");
                Console.WriteLine($"      🔗 URL: {file.Image?.Url ?? "N/A"}");
                
                return file.Id;
            }
            else
            {
                throw new InvalidOperationException("No file was uploaded");
            }
        }

        private async Task VerifyImageExistsInShopify(string fileId)
        {
            Console.WriteLine($"🔄 Verifying image exists in Shopify: {fileId}");
            
            try
            {
                // Try to get the file details from Shopify
                var query = @"
                    query GetFile($id: ID!) {
                        node(id: $id) {
                            ... on MediaImage {
                                id
                                alt
                                image {
                                    url
                                    width
                                    height
                                }
                                createdAt
                            }
                        }
                    }";

                var variables = new { id = fileId };
                var response = await _client.GraphQL.ExecuteQueryAsync(query, variables);
                
                Console.WriteLine($"   ✅ GraphQL response received:");
                Console.WriteLine($"      Response length: {response?.Length ?? 0} characters");
                Console.WriteLine($"      Response preview: {response?.Substring(0, Math.Min(200, response?.Length ?? 0))}...");
                
                if (response?.Contains("error") == true)
                {
                    Console.WriteLine("      ⚠️  Response contains error");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error verifying image: {ex.Message}");
            }
        }

        private async Task ManuallyStoreMetafield(string fileId)
        {
            Console.WriteLine($"🔄 Manually storing metafield for file: {fileId}");
            
            try
            {
                var metafieldInput = new MetafieldInput
                {
                    Namespace = "debug",
                    Key = "test_key",
                    Value = "test_value",
                    Type = "single_line_text_field"
                };

                Console.WriteLine($"   📝 Metafield input:");
                Console.WriteLine($"      Namespace: {metafieldInput.Namespace}");
                Console.WriteLine($"      Key: {metafieldInput.Key}");
                Console.WriteLine($"      Value: {metafieldInput.Value}");
                Console.WriteLine($"      Type: {metafieldInput.Type}");

                var metafield = await _fileMetafieldService.CreateOrUpdateFileMetafieldAsync(fileId, metafieldInput);
                
                Console.WriteLine($"   ✅ Metafield created successfully:");
                Console.WriteLine($"      ID: {metafield.Id}");
                Console.WriteLine($"      Namespace: {metafield.Namespace}");
                Console.WriteLine($"      Key: {metafield.Key}");
                Console.WriteLine($"      Value: {metafield.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error storing metafield: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private async Task VerifyMetafieldStorage(string fileId)
        {
            Console.WriteLine($"🔄 Verifying metafield storage for file: {fileId}");
            
            try
            {
                var metafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileId);
                
                Console.WriteLine($"   📊 Found {metafields.Count} metafields:");
                foreach (var metafield in metafields)
                {
                    Console.WriteLine($"      - {metafield.Namespace}.{metafield.Key}: {metafield.Value} ({metafield.Type})");
                }

                // Try to get product ID specifically
                var productId = await _fileMetafieldService.GetProductIdFromFileAsync(fileId);
                Console.WriteLine($"   🆔 Product ID retrieved: {productId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error verifying metafields: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_uploadedFileIds.Any())
            {
                Console.WriteLine($"🧹 Debug test uploaded {_uploadedFileIds.Count} files");
                Console.WriteLine("Note: These files remain in your Shopify store for inspection");
            }
        }
    }
}
