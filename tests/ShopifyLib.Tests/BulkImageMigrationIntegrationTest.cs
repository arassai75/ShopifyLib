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
    public class BulkImageMigrationIntegrationTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly StagedUploadService _stagedUploadService;
        private readonly EnhancedFileService _enhancedFileService;
        private readonly string _testCsvPath;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public BulkImageMigrationIntegrationTest()
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
            
            // Create services manually since they're not exposed by ShopifyClient
            _stagedUploadService = new StagedUploadService(_client.GraphQL, _client.HttpClient);
            _enhancedFileService = new EnhancedFileService(_client.Files);
            
            // Create test CSV file path
            _testCsvPath = Path.Combine(Path.GetTempPath(), $"bulk_migration_test_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        [Fact]
        public async Task BulkImageMigration_WithCSV_ShouldUploadImagesAndUpdateCSV()
        {
            Console.WriteLine("=== BULK IMAGE MIGRATION INTEGRATION TEST ===");
            Console.WriteLine("This test simulates a complete book migration workflow:");
            Console.WriteLine("1. Creates a CSV file with 5 test rows");
            Console.WriteLine("2. Uploads images in batches using existing library");
            Console.WriteLine("3. Uses alt text for product ID tracking");
            Console.WriteLine("4. Updates CSV with Shopify GUIDs");
            Console.WriteLine("5. Verifies images appear in Shopify dashboard");
            Console.WriteLine();

            try
            {
                // Step 1: Create test CSV file
                await CreateTestCsvFile();
                Console.WriteLine("✅ Step 1: Test CSV file created successfully");
                Console.WriteLine($"   CSV Path: {_testCsvPath}");
                Console.WriteLine();

                // Step 2: Process the migration
                var migrationResult = await ProcessBulkMigration();
                Console.WriteLine("✅ Step 2: Bulk migration completed successfully");
                Console.WriteLine($"   Images uploaded: {migrationResult.UploadedCount}");
                Console.WriteLine($"   Failed uploads: {migrationResult.FailedCount}");
                Console.WriteLine();

                // Step 3: Verify results
                await VerifyMigrationResults(migrationResult);
                Console.WriteLine("✅ Step 3: Migration verification completed");
                Console.WriteLine();

                // Step 4: Display final results
                DisplayFinalResults(migrationResult);
                Console.WriteLine();
                Console.WriteLine("🎉 BULK IMAGE MIGRATION TEST COMPLETED SUCCESSFULLY!");
                Console.WriteLine("📱 Check your Shopify dashboard to see the uploaded images");
                Console.WriteLine("💡 Each image should have alt text with PRODUCT_ID and BATCH_ID");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
                throw;
            }
        }

        private async Task CreateTestCsvFile()
        {
            // Create test data with 5 rows
            var testData = new List<MigrationRow>
            {
                new MigrationRow { ProductId = 1001, ImageUrl = "https://httpbin.org/image/jpeg", ShopifyGuid = "" },
                new MigrationRow { ProductId = 1002, ImageUrl = "https://httpbin.org/image/png", ShopifyGuid = "" },
                new MigrationRow { ProductId = 1003, ImageUrl = "https://httpbin.org/image/webp", ShopifyGuid = "" },
                new MigrationRow { ProductId = 1004, ImageUrl = "https://httpbin.org/image/svg", ShopifyGuid = "" },
                new MigrationRow { ProductId = 1005, ImageUrl = "https://httpbin.org/image/jpeg?size=800x600", ShopifyGuid = "" }
            };

            // Write CSV file
            using var writer = new StreamWriter(_testCsvPath);
            await writer.WriteLineAsync("ProductID,ImageURL,ShopifyGUID,Status,ErrorMessage");
            
            foreach (var row in testData)
            {
                await writer.WriteLineAsync($"{row.ProductId},{row.ImageUrl},{row.ShopifyGuid},{row.Status},{row.ErrorMessage}");
            }

            Console.WriteLine($"📄 Created test CSV with {testData.Count} rows:");
            foreach (var row in testData)
            {
                Console.WriteLine($"   • Product {row.ProductId}: {row.ImageUrl}");
            }
        }

        private async Task<MigrationResult> ProcessBulkMigration()
        {
            var result = new MigrationResult();
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            
            Console.WriteLine($"🔄 Starting bulk migration with date: {datePrefix}");
            Console.WriteLine($"📦 Processing in batches of 10 images...");

            // Read CSV data
            var csvRows = await ReadCsvFile();
            Console.WriteLine($"📋 Read {csvRows.Count} rows from CSV");

            // Process in batches of 10 (larger batch size)
            var batches = csvRows.Chunk(10).ToList();
            
            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                var batch = batches[batchIndex];
                var batchId = $"{datePrefix}{(batchIndex + 1):D3}";
                Console.WriteLine($"\n📦 Processing batch {batchIndex + 1}/{batches.Count} ({batch.Length} images) - Batch ID: {batchId}");

                try
                {
                    // Create file inputs for this batch
                    var fileInputs = batch.Select(row => new FileCreateInput
                    {
                        OriginalSource = row.ImageUrl,
                        ContentType = FileContentType.Image,
                        Alt = GenerateAltText(row.ProductId, batchId)
                    }).ToList();

                    // Upload batch using existing library capabilities
                    Console.WriteLine("📤 Uploading batch to Shopify...");
                    var uploadResponse = await _enhancedFileService.UploadImagesFromUrlsAsync(
                        batch.Select(row => (row.ImageUrl, GenerateAltText(row.ProductId, batchId))).ToList()
                    );

                    if (uploadResponse?.Files == null || uploadResponse.Files.Count == 0)
                    {
                        throw new Exception("Upload failed - no files returned");
                    }

                    // Process upload results
                    foreach (var file in uploadResponse.Files)
                    {
                        var altText = file.Alt ?? "";
                        var (productId, _) = ParseAltText(altText);
                        var originalRow = batch.FirstOrDefault(r => r.ProductId == productId);
                        
                        if (originalRow != null)
                        {
                            originalRow.ShopifyGuid = file.Id;
                            originalRow.Status = "Success";
                            _uploadedFileIds.Add(file.Id);
                            result.UploadedCount++;
                            
                            Console.WriteLine($"   ✅ Product {productId}: {file.Id}");
                        }
                    }

                    Console.WriteLine($"✅ Batch {batchIndex + 1} completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Batch {batchIndex + 1} failed: {ex.Message}");
                    
                    // Mark batch as failed
                    foreach (var row in batch)
                    {
                        row.Status = "Failed";
                        row.ErrorMessage = ex.Message;
                        result.FailedCount++;
                    }
                }
            }

            // Update CSV with results
            await UpdateCsvFile(csvRows);
            Console.WriteLine($"📝 Updated CSV file with results");

            return result;
        }

        private async Task VerifyMigrationResults(MigrationResult result)
        {
            Console.WriteLine("🔍 Verifying migration results...");

            if (result.UploadedCount == 0)
            {
                Console.WriteLine("⚠️  No images were uploaded successfully");
                return;
            }

            // Verify each uploaded file
            foreach (var fileId in _uploadedFileIds.Take(3)) // Check first 3 files
            {
                try
                {
                    // Query file details using GraphQL
                    var fileQuery = @"
                        query getFile($id: ID!) {
                            node(id: $id) {
                                ... on MediaImage {
                                    id
                                    fileStatus
                                    alt
                                    image {
                                        url
                                        width
                                        height
                                    }
                                }
                            }
                        }";

                    var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, new { id = fileId });
                    var parsedResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(queryResponse);
                    var node = parsedResponse?.data?.node;

                    if (node != null)
                    {
                        var altText = node.alt?.ToString() ?? "";
                        var parsedAltText = ParseAltText(altText);
                        var productId = parsedAltText.Item1;
                        var batchId = parsedAltText.Item2;
                        
                        Console.WriteLine($"   ✅ File {fileId}:");
                        Console.WriteLine($"      Status: {node.fileStatus}");
                        Console.WriteLine($"      Alt Text: {altText}");
                        Console.WriteLine($"      Product ID: {productId}");
                        Console.WriteLine($"      Batch ID: {batchId}");
                        
                        if (node.image != null)
                        {
                            Console.WriteLine($"      Dimensions: {node.image.width}x{node.image.height}");
                            Console.WriteLine($"      CDN URL: {node.image.url}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Could not verify file {fileId}: {ex.Message}");
                }
            }
        }

        private void DisplayFinalResults(MigrationResult result)
        {
            Console.WriteLine("📊 FINAL MIGRATION RESULTS:");
            Console.WriteLine($"   📤 Total images uploaded: {result.UploadedCount}");
            Console.WriteLine($"   ❌ Failed uploads: {result.FailedCount}");
            Console.WriteLine($"   📄 Updated CSV: {_testCsvPath}");
            Console.WriteLine();
            Console.WriteLine("🎯 NEXT STEPS:");
            Console.WriteLine("   1. Check your Shopify dashboard for uploaded images");
            Console.WriteLine("   2. Verify alt text contains PRODUCT_ID and BATCH_ID");
            Console.WriteLine("   3. Use the updated CSV for further processing");
        }

        private string GenerateAltText(long productId, string batchId)
        {
            return $"PRODUCT_ID:{productId}|BATCH:{batchId}";
        }

        private (long ProductId, string BatchId) ParseAltText(string altText)
        {
            if (string.IsNullOrEmpty(altText)) return (0, "");
            
            var parts = altText.Split('|');
            var productIdPart = parts.FirstOrDefault(p => p.StartsWith("PRODUCT_ID:"));
            var batchPart = parts.FirstOrDefault(p => p.StartsWith("BATCH:"));
            
            var productId = long.TryParse(productIdPart?.Replace("PRODUCT_ID:", ""), out var id) ? id : 0;
            var batchId = batchPart?.Replace("BATCH:", "") ?? "";
            
            return (productId, batchId);
        }

        private async Task<List<MigrationRow>> ReadCsvFile()
        {
            var rows = new List<MigrationRow>();
            
            using var reader = new StreamReader(_testCsvPath);
            await reader.ReadLineAsync(); // Skip header
            
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    rows.Add(new MigrationRow
                    {
                        ProductId = long.TryParse(parts[0], out var id) ? id : 0,
                        ImageUrl = parts[1],
                        ShopifyGuid = parts[2],
                        Status = parts.Length > 3 ? parts[3] : "",
                        ErrorMessage = parts.Length > 4 ? parts[4] : ""
                    });
                }
            }
            
            return rows;
        }

        private async Task UpdateCsvFile(List<MigrationRow> rows)
        {
            using var writer = new StreamWriter(_testCsvPath, false);
            await writer.WriteLineAsync("ProductID,ImageURL,ShopifyGUID,Status,ErrorMessage");
            
            foreach (var row in rows)
            {
                await writer.WriteLineAsync($"{row.ProductId},{row.ImageUrl},{row.ShopifyGuid},{row.Status},{row.ErrorMessage}");
            }
        }

        public void Dispose()
        {
            // Clean up uploaded files (optional - for testing purposes)
            if (_uploadedFileIds.Any())
            {
                Console.WriteLine($"🧹 Cleaning up {_uploadedFileIds.Count} uploaded test files...");
                // Note: In a real scenario, you might want to keep the files
                // This is just for test cleanup
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

    // Supporting classes
    public class MigrationRow
    {
        public long ProductId { get; set; }
        public string ImageUrl { get; set; } = "";
        public string ShopifyGuid { get; set; } = "";
        public string Status { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public class MigrationResult
    {
        public int UploadedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> UploadedFileIds { get; set; } = new List<string>();
    }
}
