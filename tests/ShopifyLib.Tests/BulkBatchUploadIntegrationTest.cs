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
    /// Test demonstrating bulk image upload with product ID, UPC, and batch ID metadata storage
    /// </summary>
    [IntegrationTest]
    public class BulkBatchUploadIntegrationTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _testCsvPaths = new List<string>();

        public BulkBatchUploadIntegrationTest()
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
        public async Task BulkBatchUpload_ShouldUploadImagesWithMetadata()
        {
            Console.WriteLine("BULK BATCH UPLOAD INTEGRATION TEST");
            Console.WriteLine("==================================");
            Console.WriteLine("This test demonstrates uploading a batch of images with product ID, UPC, and batch ID metadata storage");
            Console.WriteLine();

            try
            {
                // Step 1: Create CSV file with test data
                var csvPath = await CreateTestCsvFile();
                Console.WriteLine("Step 1: Created test CSV file");
                Console.WriteLine();

                // Step 2: Read CSV and prepare image data
                var imageData = await ReadCsvAndPrepareImageData(csvPath);
                Console.WriteLine($"Step 2: Prepared {imageData.Count} images for upload");
                Console.WriteLine();

                // Step 3: Upload images with metadata
                var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);
                Console.WriteLine("Step 3: Uploaded images with metadata");
                Console.WriteLine();

                // Step 4: Display results
                DisplayUploadResults(response);
                Console.WriteLine();

                // Step 5: Verify metadata storage (direct read from file IDs)
                await VerifyMetadataStorage(response.Files);
                Console.WriteLine();

                // Step 6: Demonstrate metadata retrieval (direct read from file IDs)
                await DemonstrateMetadataRetrieval(response.Files);
                Console.WriteLine();

                Console.WriteLine("BULK BATCH UPLOAD TEST COMPLETED SUCCESSFULLY");
                Console.WriteLine($"Total uploaded: {response.Files.Count} images");
                Console.WriteLine("All product IDs, UPCs, and batch IDs stored in metafields");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed with error: {ex.Message}");
                throw;
            }
        }

        private async Task<string> CreateTestCsvFile()
        {
            var csvPath = Path.GetTempFileName();
            _testCsvPaths.Add(csvPath);

            using var writer = new StreamWriter(csvPath);
            await writer.WriteLineAsync("ProductID,UPC,ProductName,ImageURL");

            // Create 15 test products
            var testProducts = new[]
            {
                new { ProductId = 1001L, Upc = "123456789012", ProductName = "Blue T-Shirt", ImageUrl = "https://httpbin.org/image/jpeg" },
                new { ProductId = 1002L, Upc = "123456789013", ProductName = "Red Jeans", ImageUrl = "https://httpbin.org/image/png" },
                new { ProductId = 1003L, Upc = "123456789014", ProductName = "Green Hat", ImageUrl = "https://httpbin.org/image/webp" },
                new { ProductId = 1004L, Upc = "123456789015", ProductName = "Black Shoes", ImageUrl = "https://httpbin.org/image/jpeg" },
                new { ProductId = 1005L, Upc = "123456789016", ProductName = "White Socks", ImageUrl = "https://httpbin.org/image/png" },
                new { ProductId = 2001L, Upc = "234567890123", ProductName = "Smartphone", ImageUrl = "https://httpbin.org/image/jpeg" },
                new { ProductId = 2002L, Upc = "234567890124", ProductName = "Laptop", ImageUrl = "https://httpbin.org/image/png" },
                new { ProductId = 2003L, Upc = "234567890125", ProductName = "Headphones", ImageUrl = "https://httpbin.org/image/webp" },
                new { ProductId = 2004L, Upc = "234567890126", ProductName = "Tablet", ImageUrl = "https://httpbin.org/image/jpeg" },
                new { ProductId = 2005L, Upc = "234567890127", ProductName = "Camera", ImageUrl = "https://httpbin.org/image/png" },
                new { ProductId = 3001L, Upc = "345678901234", ProductName = "Coffee Maker", ImageUrl = "https://httpbin.org/image/jpeg" },
                new { ProductId = 3002L, Upc = "345678901235", ProductName = "Blender", ImageUrl = "https://httpbin.org/image/png" },
                new { ProductId = 3003L, Upc = "345678901236", ProductName = "Toaster", ImageUrl = "https://httpbin.org/image/webp" },
                new { ProductId = 3004L, Upc = "345678901237", ProductName = "Microwave", ImageUrl = "https://httpbin.org/image/jpeg" },
                new { ProductId = 3005L, Upc = "345678901238", ProductName = "Dishwasher", ImageUrl = "https://httpbin.org/image/png" }
            };

            foreach (var product in testProducts)
            {
                await writer.WriteLineAsync($"{product.ProductId},{product.Upc},{product.ProductName},{product.ImageUrl}");
            }

            Console.WriteLine($"Created CSV file: {csvPath}");
            Console.WriteLine($"Added {testProducts.Length} products to CSV");
            return csvPath;
        }

        private async Task<List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>> ReadCsvAndPrepareImageData(string csvPath)
        {
            var imageData = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>();
            var batchId = $"batch_{DateTime.UtcNow:yyyyMMddHHmmss}";

            using var reader = new StreamReader(csvPath);
            await reader.ReadLineAsync(); // Skip header

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    var productId = long.Parse(parts[0]);
                    var upc = parts[1];
                    var productName = parts[2];
                    var imageUrl = parts[3];

                    imageData.Add((
                        imageUrl,
                        FileContentType.Image,
                        productId,
                        upc,
                        batchId,
                        $"Product image for {productName}"
                    ));
                }
            }

            Console.WriteLine($"Read {imageData.Count} products from CSV");
            Console.WriteLine($"Batch ID: {batchId}");
            return imageData;
        }

        private void DisplayUploadResults(FileCreateResponseWithMetadata response)
        {
            Console.WriteLine("UPLOAD RESULTS:");
            Console.WriteLine("===============");
            Console.WriteLine($"Total Files: {response.Summary.TotalFiles}");
            Console.WriteLine($"Successfully Uploaded: {response.Summary.SuccessfullyUploaded}");
            Console.WriteLine($"Failed Uploads: {response.Summary.FailedUploads}");
            Console.WriteLine($"Metadata Stored: {response.Summary.MetadataStored}");
            Console.WriteLine($"Metadata Storage Failed: {response.Summary.MetadataStorageFailed}");
            Console.WriteLine($"Batch ID: {response.Summary.BatchId}");
            Console.WriteLine();

            Console.WriteLine("UPLOADED FILES:");
            Console.WriteLine("===============");
            foreach (var file in response.Files)
            {
                Console.WriteLine($"File ID: {file.Id}");
                Console.WriteLine($"  Product ID: {file.ProductId}");
                Console.WriteLine($"  UPC: {file.Upc}");
                Console.WriteLine($"  Batch ID: {file.BatchId}");
                Console.WriteLine($"  Metadata Stored: {file.MetadataStored}");
                if (!string.IsNullOrEmpty(file.MetadataError))
                {
                    Console.WriteLine($"  Metadata Error: {file.MetadataError}");
                }
                Console.WriteLine();
            }
        }

        private async Task VerifyMetadataStorage(List<FileWithMetadata> files)
        {
            Console.WriteLine("VERIFYING METADATA STORAGE:");
            Console.WriteLine("============================");

            foreach (var file in files.Take(5)) // Verify first 5 files
            {
                var productId = await _enhancedFileService.GetProductIdFromFileAsync(file.Id);
                var upc = await _enhancedFileService.GetUpcFromFileAsync(file.Id);
                var allMetadata = await _enhancedFileService.GetFileMetadataAsync(file.Id);

                Console.WriteLine($"File: {file.Id}");
                Console.WriteLine($"  Expected Product ID: {file.ProductId}, Retrieved: {productId}");
                Console.WriteLine($"  Expected UPC: {file.Upc}, Retrieved: {upc}");
                Console.WriteLine($"  Total metafields: {allMetadata.Count}");
                Console.WriteLine();
            }
        }

        private async Task DemonstrateMetadataRetrieval(List<FileWithMetadata> files)
        {
            Console.WriteLine("DEMONSTRATING METADATA RETRIEVAL:");
            Console.WriteLine("=================================");

            // Demonstrate getting all metadata for a file using its GID
            var testFile = files.First();
            var allMetadata = await _enhancedFileService.GetFileMetadataAsync(testFile.Id);
            Console.WriteLine($"File {testFile.Id} has {allMetadata.Count} metafields:");
            foreach (var metafield in allMetadata)
            {
                Console.WriteLine($"  {metafield.Namespace}.{metafield.Key}: {metafield.Value} ({metafield.Type})");
            }

            // Demonstrate retrieving specific metadata by GID
            var productId = await _enhancedFileService.GetProductIdFromFileAsync(testFile.Id);
            var upc = await _enhancedFileService.GetUpcFromFileAsync(testFile.Id);
            Console.WriteLine($"Retrieved Product ID: {productId}");
            Console.WriteLine($"Retrieved UPC: {upc}");
        }

        public void Dispose()
        {
            // Cleanup CSV files
            foreach (var csvPath in _testCsvPaths)
            {
                try
                {
                    if (System.IO.File.Exists(csvPath))
                    {
                        System.IO.File.Delete(csvPath);
                        Console.WriteLine($"Cleaned up test CSV file: {csvPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to cleanup CSV file {csvPath}: {ex.Message}");
                }
            }
        }
    }
}
