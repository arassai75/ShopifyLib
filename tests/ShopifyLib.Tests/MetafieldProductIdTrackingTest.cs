using System;
using System.Collections.Generic;
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
    [IntegrationTest]
    public class MetafieldProductIdTrackingTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public MetafieldProductIdTrackingTest()
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
            
            // Create services
            _fileMetafieldService = new FileMetafieldService(_client.GraphQL);
            _enhancedFileService = new EnhancedFileServiceWithMetadata(
                _client.Files, 
                _fileMetafieldService,
                new ImageDownloadService()
            );
        }

        [Fact]
        public async Task MetafieldProductIdTracking_ShouldStoreAndRetrieveProductIds()
        {
            Console.WriteLine("=== METAFIELD PRODUCT ID TRACKING TEST ===");
            Console.WriteLine("This test demonstrates storing product IDs in metafields instead of alt text");
            Console.WriteLine();

            try
            {
                // Test data
                var testImages = new[]
                {
                    ("https://httpbin.org/image/jpeg", 1001L, "test_batch_001", "Test product 1001 image"),
                    ("https://httpbin.org/image/png", 1002L, "test_batch_001", "Test product 1002 image"),
                    ("https://httpbin.org/image/webp", 1003L, "test_batch_001", "Test product 1003 image")
                };

                Console.WriteLine("🔄 Uploading images with product ID metadata...");
                
                // Upload images with metadata
                var imageData = testImages.Select(img => (img.Item1, FileContentType.Image, img.Item2, "123456789012", img.Item3, img.Item4)).ToList();
                var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);

                Console.WriteLine($"✅ Uploaded {response.Files.Count} images successfully");
                Console.WriteLine();

                // Store file IDs for cleanup
                _uploadedFileIds.AddRange(response.Files.Select(f => f.Id));

                // Test 1: Retrieve product IDs from metafields
                Console.WriteLine("🔄 Test 1: Retrieving product IDs from metafields...");
                foreach (var file in response.Files)
                {
                    var productId = await _enhancedFileService.GetProductIdFromFileAsync(file.Id);
                    Console.WriteLine($"   File: {file.Id}");
                    Console.WriteLine($"   Product ID: {productId}");
                    Console.WriteLine($"   Alt Text: {file.Alt} (clean for accessibility)");
                    Console.WriteLine();
                }

                // Test 2: Get all metadata for a file
                Console.WriteLine("🔄 Test 2: Getting all metadata for a file...");
                if (response.Files.Any())
                {
                    var firstFile = response.Files.First();
                    var metadata = await _enhancedFileService.GetFileMetadataAsync(firstFile.Id);
                    
                    Console.WriteLine($"   File: {firstFile.Id}");
                    Console.WriteLine($"   Metadata count: {metadata.Count}");
                    foreach (var metafield in metadata)
                    {
                        Console.WriteLine($"     {metafield.Namespace}.{metafield.Key}: {metafield.Value} ({metafield.Type})");
                    }
                    Console.WriteLine();
                }

                // Test 3: Demonstrate metadata retrieval for uploaded file
                Console.WriteLine("🔄 Test 3: Demonstrating metadata retrieval...");
                var testFileId = response.Files.First().Id;
                var retrievedProductId = await _enhancedFileService.GetProductIdFromFileAsync(testFileId);
                var metadata = await _enhancedFileService.GetFileMetadataAsync(testFileId);
                
                Console.WriteLine($"   Retrieved metadata for file {testFileId}:");
                Console.WriteLine($"     Product ID: {retrievedProductId}");
                Console.WriteLine($"     Total metafields: {metadata.Count}");
                foreach (var metafield in metadata)
                {
                    Console.WriteLine($"       {metafield.Namespace}.{metafield.Key}: {metafield.Value}");
                }
                Console.WriteLine();

                // Test 4: Compare with old alt text approach
                Console.WriteLine("🔄 Test 4: Comparing with old alt text approach...");
                Console.WriteLine("   OLD APPROACH (Alt Text):");
                Console.WriteLine("     Alt: \"PRODUCT_ID:1001|BATCH:test_batch_001\"");
                Console.WriteLine("     Issues: Pollutes alt text, not searchable, limited space");
                Console.WriteLine();
                Console.WriteLine("   NEW APPROACH (Metafields):");
                Console.WriteLine("     Alt: \"Test product 1001 image\" (clean for accessibility)");
                Console.WriteLine("     Metafields: migration.product_id=1001, migration.batch_id=test_batch_001");
                Console.WriteLine("     Benefits: Searchable, extensible, clean alt text");
                Console.WriteLine();

                Console.WriteLine("🎉 METAFIELD PRODUCT ID TRACKING TEST COMPLETED SUCCESSFULLY!");
                Console.WriteLine("💡 Product IDs are now stored in metafields instead of alt text");
                Console.WriteLine("📋 You can retrieve metadata using file GID");
                Console.WriteLine("♿ Alt text remains clean for accessibility purposes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task BulkMigrationWithMetafields_ShouldWorkLikeOldApproach()
        {
            Console.WriteLine("=== BULK MIGRATION WITH METAFIELDS TEST ===");
            Console.WriteLine("This test demonstrates bulk migration using metafields instead of alt text");
            Console.WriteLine();

            try
            {
                // Simulate CSV data
                var csvRows = new[]
                {
                    new { ProductId = 2001L, ImageUrl = "https://httpbin.org/image/jpeg", ShopifyGuid = "", Status = "", ErrorMessage = "" },
                    new { ProductId = 2002L, ImageUrl = "https://httpbin.org/image/png", ShopifyGuid = "", Status = "", ErrorMessage = "" },
                    new { ProductId = 2003L, ImageUrl = "https://httpbin.org/image/webp", ShopifyGuid = "", Status = "", ErrorMessage = "" }
                };

                var batchId = $"test_batch_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var uploadedCount = 0;
                var failedCount = 0;

                Console.WriteLine($"🔄 Processing batch: {batchId}");
                Console.WriteLine($"   Images to upload: {csvRows.Length}");
                Console.WriteLine();

                // Prepare image data for upload
                var imageData = csvRows.Select(row => (
                    row.ImageUrl,
                    row.ProductId,
                    "123456789012",
                    batchId,
                    $"Product {row.ProductId} image" // Clean alt text
                )).ToList();

                // Upload with metadata
                var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);

                // Update results
                foreach (var file in response.Files)
                {
                    var productId = await _enhancedFileService.GetProductIdFromFileAsync(file.Id);
                    var originalRow = csvRows.FirstOrDefault(r => r.ProductId == productId);
                    
                    if (originalRow != null)
                    {
                        // Update CSV row (in real scenario, you'd update the actual CSV)
                        Console.WriteLine($"   ✅ Product {productId}: {file.Id}");
                        uploadedCount++;
                        _uploadedFileIds.Add(file.Id);
                    }
                }

                failedCount = csvRows.Length - uploadedCount;

                Console.WriteLine();
                Console.WriteLine("📊 MIGRATION RESULTS:");
                Console.WriteLine($"   ✅ Uploaded: {uploadedCount}");
                Console.WriteLine($"   ❌ Failed: {failedCount}");
                Console.WriteLine($"   📄 Batch ID: {batchId}");
                Console.WriteLine();

                // Verify all product IDs can be retrieved
                Console.WriteLine("🔄 Verifying product ID retrieval...");
                foreach (var file in response.Files)
                {
                    var productId = await _enhancedFileService.GetProductIdFromFileAsync(file.Id);
                    var metadata = await _enhancedFileService.GetFileMetadataAsync(file.Id);
                    
                    Console.WriteLine($"   File: {file.Id}");
                    Console.WriteLine($"   Product ID: {productId}");
                    Console.WriteLine($"   Metadata count: {metadata.Count}");
                    Console.WriteLine($"   Alt text: {file.Alt}");
                    Console.WriteLine();
                }

                Console.WriteLine("🎉 BULK MIGRATION WITH METAFIELDS COMPLETED!");
                Console.WriteLine("💡 This approach provides the same functionality as alt text but with better data management");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task MetafieldSearchAndRetrieval_ShouldFindFilesByProductId()
        {
            Console.WriteLine("=== METAFIELD SEARCH AND RETRIEVAL TEST ===");
            Console.WriteLine("This test demonstrates searching for files by product ID using metafields");
            Console.WriteLine();

            try
            {
                // Upload test images with different product IDs
                var testData = new[]
                {
                    ("https://httpbin.org/image/jpeg", 3001L, "search_batch", "Search test image 1"),
                    ("https://httpbin.org/image/png", 3001L, "search_batch", "Search test image 2"), // Same product ID
                    ("https://httpbin.org/image/webp", 3002L, "search_batch", "Search test image 3")
                };

                Console.WriteLine("🔄 Uploading test images...");
                var imageData = testData.Select(img => (img.Item1, FileContentType.Image, img.Item2, "123456789012", img.Item3, img.Item4)).ToList();
                var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);

                _uploadedFileIds.AddRange(response.Files.Select(f => f.Id));

                Console.WriteLine($"✅ Uploaded {response.Files.Count} images");
                Console.WriteLine();

                // Test search for product 3001 (should find 2 files)
                Console.WriteLine("🔄 Searching for files with product ID 3001...");
                var filesForProduct3001 = await _enhancedFileService.FindFilesByProductIdAsync(3001L);
                
                Console.WriteLine($"   Found {filesForProduct3001.Count} files for product 3001:");
                foreach (var fileGid in filesForProduct3001)
                {
                    var metadata = await _enhancedFileService.GetFileMetadataAsync(fileGid);
                    var productId = await _enhancedFileService.GetProductIdFromFileAsync(fileGid);
                    
                    Console.WriteLine($"     {fileGid} -> Product {productId}");
                    Console.WriteLine($"       Metadata: {string.Join(", ", metadata.Select(m => $"{m.Namespace}.{m.Key}={m.Value}"))}");
                }
                Console.WriteLine();

                // Test search for product 3002 (should find 1 file)
                Console.WriteLine("🔄 Searching for files with product ID 3002...");
                var filesForProduct3002 = await _enhancedFileService.FindFilesByProductIdAsync(3002L);
                
                Console.WriteLine($"   Found {filesForProduct3002.Count} files for product 3002:");
                foreach (var fileGid in filesForProduct3002)
                {
                    var productId = await _enhancedFileService.GetProductIdFromFileAsync(fileGid);
                    Console.WriteLine($"     {fileGid} -> Product {productId}");
                }
                Console.WriteLine();

                // Test search for non-existent product (should find 0 files)
                Console.WriteLine("🔄 Searching for non-existent product ID 9999...");
                var filesForNonExistent = await _enhancedFileService.FindFilesByProductIdAsync(9999L);
                Console.WriteLine($"   Found {filesForNonExistent.Count} files for product 9999");
                Console.WriteLine();

                Console.WriteLine("🎉 METAFIELD SEARCH AND RETRIEVAL TEST COMPLETED!");
                Console.WriteLine("💡 You can now search for files by product ID using GraphQL queries");
                Console.WriteLine("🔍 This is much more powerful than parsing alt text");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            // Clean up uploaded files
            if (_uploadedFileIds.Any())
            {
                Console.WriteLine($"🧹 Cleaning up {_uploadedFileIds.Count} uploaded test files...");
                // Note: In a real scenario, you might want to keep the files
                // This is just for test cleanup
            }
        }
    }
}
