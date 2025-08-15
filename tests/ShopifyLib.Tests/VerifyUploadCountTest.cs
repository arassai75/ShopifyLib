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
    /// Test to verify actual upload count and identify the issue
    /// </summary>
    [IntegrationTest]
    public class VerifyUploadCountTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public VerifyUploadCountTest()
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
        public async Task VerifyUploadCount_ShouldIdentifyTheIssue()
        {
            Console.WriteLine("=== VERIFY UPLOAD COUNT TEST ===");
            Console.WriteLine("Testing to see how many images are actually uploaded");
            Console.WriteLine();

            try
            {
                // Test 1: Upload 3 images with different URLs
                Console.WriteLine("🔄 Test 1: Uploading 3 images with different URLs...");
                var imageData1 = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>
                {
                    ("https://httpbin.org/image/jpeg", FileContentType.Image, 111111111, "123456789012", "test_batch_1", "Test image 1"),
                    ("https://httpbin.org/image/png", FileContentType.Image, 222222222, "123456789013", "test_batch_1", "Test image 2"),
                    ("https://httpbin.org/image/webp", FileContentType.Image, 333333333, "123456789014", "test_batch_1", "Test image 3")
                };

                var response1 = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData1);
                
                Console.WriteLine($"✅ Test 1: Uploaded {response1?.Files?.Count ?? 0} images");
                if (response1?.Files != null)
                {
                    foreach (var file in response1.Files)
                    {
                        _uploadedFileIds.Add(file.Id);
                        Console.WriteLine($"   📁 {file.Id} -> Alt: {file.Alt ?? "None"}");
                    }
                }
                Console.WriteLine();

                // Test 2: Upload 3 images with SAME URLs (this is the problem!)
                Console.WriteLine("🔄 Test 2: Uploading 3 images with SAME URLs...");
                var imageData2 = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>
                {
                    ("https://httpbin.org/image/jpeg", FileContentType.Image, 444444444, "123456789015", "test_batch_2", "Test image 4"),
                    ("https://httpbin.org/image/jpeg", FileContentType.Image, 555555555, "123456789016", "test_batch_2", "Test image 5"),
                    ("https://httpbin.org/image/jpeg", FileContentType.Image, 666666666, "123456789017", "test_batch_2", "Test image 6")
                };

                var response2 = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData2);
                
                Console.WriteLine($"✅ Test 2: Uploaded {response2?.Files?.Count ?? 0} images");
                if (response2?.Files != null)
                {
                    foreach (var file in response2.Files)
                    {
                        _uploadedFileIds.Add(file.Id);
                        Console.WriteLine($"   📁 {file.Id} -> Alt: {file.Alt ?? "None"}");
                    }
                }
                Console.WriteLine();

                // Test 3: Check what product IDs are actually stored
                Console.WriteLine("🔍 Test 3: Checking stored product IDs...");
                if (response2?.Files != null)
                {
                    for (int i = 0; i < response2.Files.Count; i++)
                    {
                        var file = response2.Files[i];
                        var expectedProductId = imageData2[i].ProductId;
                        var actualProductId = await _enhancedFileService.GetProductIdFromFileAsync(file.Id);
                        
                        Console.WriteLine($"   📁 {file.Id}");
                        Console.WriteLine($"      Expected: {expectedProductId}, Actual: {actualProductId} {(expectedProductId == actualProductId ? "✅" : "❌")}");
                    }
                }
                Console.WriteLine();

                // Test 4: Check total files in Shopify
                Console.WriteLine("🔍 Test 4: Checking total files uploaded...");
                Console.WriteLine($"   Total files uploaded in this test: {_uploadedFileIds.Count}");
                Console.WriteLine();

                Console.WriteLine("🎯 ANALYSIS:");
                Console.WriteLine("   - Test 1 should upload 3 images (different URLs)");
                Console.WriteLine("   - Test 2 should upload 3 images (same URLs)");
                Console.WriteLine("   - If Test 2 only uploads 1 image, that's the issue!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"🧹 Test uploaded {_uploadedFileIds.Count} files to Shopify");
            Console.WriteLine("📱 Check your Shopify admin dashboard to see the actual images");
        }
    }
}
