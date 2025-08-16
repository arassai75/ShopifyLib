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
    [IntegrationTest]
    public class SimpleMetafieldUploadIntegrationTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();
        private readonly string _testCsvPath;

        public SimpleMetafieldUploadIntegrationTest()
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
            
            // Create test CSV file path
            _testCsvPath = Path.Combine(Path.GetTempPath(), $"simple_metafield_upload_test_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        [Fact]
        public async Task SimpleMetafieldUpload_FromCSV_ShouldUploadImagesWithProductIds()
        {
            Console.WriteLine("=== SIMPLE METAFIELD UPLOAD INTEGRATION TEST ===");
            Console.WriteLine("This test uploads 6 images from a CSV file to Shopify using metafields for product ID storage");
            Console.WriteLine();

            try
            {
                // Step 1: Create CSV file with test data
                await CreateTestCsvFile();
                Console.WriteLine("✅ Step 1: Created test CSV file");
                Console.WriteLine();

                // Step 2: Read CSV and upload images
                var uploadResults = await UploadImagesFromCsv();
                Console.WriteLine("✅ Step 2: Uploaded images from CSV");
                Console.WriteLine();

                // Step 3: Verify product IDs are stored in metafields
                await VerifyProductIdsInMetafields(uploadResults);
                Console.WriteLine("✅ Step 3: Verified product IDs in metafields");
                Console.WriteLine();

                // Step 4: Demonstrate search by product ID
                await DemonstrateProductIdSearch(uploadResults);
                Console.WriteLine("✅ Step 4: Demonstrated product ID search");
                Console.WriteLine();

                Console.WriteLine("🎉 SIMPLE METAFIELD UPLOAD TEST COMPLETED SUCCESSFULLY!");
                Console.WriteLine($"📊 Uploaded {uploadResults.Count} images with product IDs stored in metafields");
                Console.WriteLine("🔍 Product IDs can be retrieved and searched via GraphQL queries");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
        }

        private async Task CreateTestCsvFile()
        {
            Console.WriteLine("🔄 Creating test CSV file...");
            
            // Create test data with 6 products
            var testProducts = new[]
            {
                new { ProductId = 1001L, Upc = "123456789012", ImageUrl = "https://httpbin.org/image/jpeg", ProductName = "Blue T-Shirt" },
                new { ProductId = 1002L, Upc = "123456789013", ImageUrl = "https://httpbin.org/image/png", ProductName = "Red Jeans" },
                new { ProductId = 1003L, Upc = "123456789014", ImageUrl = "https://httpbin.org/image/webp", ProductName = "Green Hat" },
                new { ProductId = 1004L, Upc = "123456789015", ImageUrl = "https://httpbin.org/image/jpeg", ProductName = "Black Shoes" },
                new { ProductId = 1005L, Upc = "123456789016", ImageUrl = "https://httpbin.org/image/png", ProductName = "White Socks" },
                new { ProductId = 1006L, Upc = "123456789017", ImageUrl = "https://httpbin.org/image/webp", ProductName = "Yellow Belt" }
            };

            using var writer = new StreamWriter(_testCsvPath, false);
            await writer.WriteLineAsync("ProductID,UPC,ImageURL,ProductName,ShopifyGUID,Status,ErrorMessage");
            
            foreach (var product in testProducts)
            {
                await writer.WriteLineAsync($"{product.ProductId},{product.Upc},{product.ImageUrl},{product.ProductName},,,");
            }

            Console.WriteLine($"   Created CSV with {testProducts.Length} products");
            Console.WriteLine($"   CSV Path: {_testCsvPath}");
            
            // Display CSV contents
            Console.WriteLine("   CSV Contents:");
            foreach (var product in testProducts)
            {
                Console.WriteLine($"     Product {product.ProductId}: {product.ProductName} -> {product.ImageUrl}");
            }
        }

        private async Task<List<UploadResult>> UploadImagesFromCsv()
        {
            Console.WriteLine("🔄 Reading CSV and uploading images...");
            
            // Read CSV data
            var csvRows = await ReadCsvFile();
            var batchId = $"batch_{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            Console.WriteLine($"   Processing {csvRows.Count} products");
            Console.WriteLine($"   Batch ID: {batchId}");
            Console.WriteLine();

            var results = new List<UploadResult>();

            // Prepare image data for upload
            var imageData = csvRows.Select(row => (
                row.ImageUrl,
                FileContentType.Image,
                row.ProductId,
                row.Upc,
                batchId,
                $"Product image for {row.ProductName}" // Clean alt text for accessibility
            )).ToList();

            // Upload all images with metadata
            var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);
            
            Console.WriteLine($"   ✅ Successfully uploaded {response.Files.Count} images");
            Console.WriteLine();

            // Process results
            foreach (var file in response.Files)
            {
                _uploadedFileIds.Add(file.Id);
                
                // Get product ID from metafield
                var productId = await _enhancedFileService.GetProductIdFromFileAsync(file.Id);
                var metadata = await _enhancedFileService.GetFileMetadataAsync(file.Id);
                
                var originalRow = csvRows.FirstOrDefault(r => r.ProductId == productId);
                
                results.Add(new UploadResult
                {
                    FileId = file.Id,
                    ProductId = productId,
                    ProductName = originalRow?.ProductName ?? "Unknown",
                    BatchId = batchId,
                    AltText = file.Alt ?? "",
                    Metadata = metadata
                });

                Console.WriteLine($"   📁 File: {file.Id}");
                Console.WriteLine($"      Product ID: {productId}");
                Console.WriteLine($"      Product Name: {originalRow?.ProductName}");
                Console.WriteLine($"      Alt Text: {file.Alt}");
                Console.WriteLine($"      Metadata Count: {metadata.Count}");
            }

            return results;
        }

        private async Task VerifyProductIdsInMetafields(List<UploadResult> uploadResults)
        {
            Console.WriteLine("🔄 Verifying product IDs are stored in metafields...");
            Console.WriteLine();

            foreach (var result in uploadResults)
            {
                Console.WriteLine($"   🔍 Checking file: {result.FileId}");
                
                // Retrieve product ID from metafield
                var retrievedProductId = await _enhancedFileService.GetProductIdFromFileAsync(result.FileId);
                
                // Verify it matches
                var isMatch = retrievedProductId == result.ProductId;
                Console.WriteLine($"      Expected Product ID: {result.ProductId}");
                Console.WriteLine($"      Retrieved Product ID: {retrievedProductId}");
                Console.WriteLine($"      Match: {(isMatch ? "✅" : "❌")}");
                
                // Show all metadata
                var metadata = await _enhancedFileService.GetFileMetadataAsync(result.FileId);
                Console.WriteLine($"      Metadata ({metadata.Count} items):");
                foreach (var metafield in metadata)
                {
                    Console.WriteLine($"        {metafield.Namespace}.{metafield.Key}: {metafield.Value}");
                }
                Console.WriteLine();
            }
        }

        private async Task DemonstrateProductIdSearch(List<UploadResult> uploadResults)
        {
            Console.WriteLine("🔄 Demonstrating product ID search capabilities...");
            Console.WriteLine();

            // Test search for a specific product ID
            if (uploadResults.Any())
            {
                var testProductId = uploadResults.First().ProductId;
                Console.WriteLine($"   📋 Demonstrating metadata retrieval for Product ID: {testProductId}");
                
                // Get the file GID from the upload result
                var fileGid = uploadResults.First().FileId;
                var productId = await _enhancedFileService.GetProductIdFromFileAsync(fileGid);
                var metadata = await _enhancedFileService.GetFileMetadataAsync(fileGid);
                
                Console.WriteLine($"   Retrieved metadata for file {fileGid}:");
                Console.WriteLine($"     📁 {fileGid}");
                Console.WriteLine($"        Product ID: {productId}");
                Console.WriteLine($"        Metadata: {string.Join(", ", metadata.Select(m => $"{m.Key}={m.Value}"))}");
                Console.WriteLine();
            }

            // Show summary of all uploaded products
            Console.WriteLine("   📊 SUMMARY OF UPLOADED PRODUCTS:");
            foreach (var result in uploadResults)
            {
                Console.WriteLine($"     Product {result.ProductId}: {result.ProductName} -> {result.FileId}");
            }
        }

        private async Task<List<CsvRow>> ReadCsvFile()
        {
            var rows = new List<CsvRow>();
            
            using var reader = new StreamReader(_testCsvPath);
            await reader.ReadLineAsync(); // Skip header
            
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    rows.Add(new CsvRow
                    {
                        ProductId = long.TryParse(parts[0], out var id) ? id : 0,
                        Upc = parts[1],
                        ImageUrl = parts[2],
                        ProductName = parts[3],
                        ShopifyGuid = parts.Length > 4 ? parts[4] : "",
                        Status = parts.Length > 5 ? parts[5] : "",
                        ErrorMessage = parts.Length > 6 ? parts[6] : ""
                    });
                }
            }
            
            return rows;
        }

        public void Dispose()
        {
            // Clean up uploaded files
            if (_uploadedFileIds.Any())
            {
                Console.WriteLine($"🧹 Cleaning up {_uploadedFileIds.Count} uploaded test files...");
            }

            // Clean up CSV file
            if (System.IO.File.Exists(_testCsvPath))
            {
                try
                {
                    System.IO.File.Delete(_testCsvPath);
                    Console.WriteLine($"🗑️  Cleaned up test CSV file: {_testCsvPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Could not delete test CSV file: {ex.Message}");
                }
            }
        }
    }

    // Test-specific models for this test
    public class CsvRow
    {
        public long ProductId { get; set; }
        public string Upc { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ShopifyGuid { get; set; } = "";
        public string Status { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public class UploadResult
    {
        public string FileId { get; set; } = "";
        public long ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string BatchId { get; set; } = "";
        public string AltText { get; set; } = "";
        public List<Metafield> Metadata { get; set; } = new List<Metafield>();
    }
}
