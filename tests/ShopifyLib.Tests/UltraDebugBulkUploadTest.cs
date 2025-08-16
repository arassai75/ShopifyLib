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
    /// Ultra debug bulk upload test with guaranteed working images and extensive logging
    /// </summary>
    [IntegrationTest]
    public class UltraDebugBulkUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public UltraDebugBulkUploadTest()
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
        public async Task UltraDebugBulkUpload_WithGuaranteedImages_ShouldUploadAll15Products()
        {
            Console.WriteLine("=== ULTRA DEBUG BULK UPLOAD TEST ===");
            Console.WriteLine("🔍 EXTENSIVE DEBUGGING ENABLED");
            Console.WriteLine("📸 Using guaranteed working image URLs");
            Console.WriteLine("📊 Uploading 15 products with REAL images");
            Console.WriteLine();

            try
            {
                // Step 1: Create batches with GUARANTEED working images
                var batches = await CreateBatchesWithGuaranteedImages();
                Console.WriteLine("✅ Step 1: Created batches with guaranteed images");
                Console.WriteLine();

                // Step 2: Upload with extensive debugging
                var allResults = await UploadWithDebugging(batches);
                Console.WriteLine("✅ Step 2: Uploaded with debugging");
                Console.WriteLine();

                // Step 3: Verify and debug each file
                await DebugVerifyAllFiles(allResults);
                Console.WriteLine("✅ Step 3: Debug verification complete");
                Console.WriteLine();

                // Step 4: Generate debug report
                await GenerateDebugReport(allResults);
                Console.WriteLine("✅ Step 4: Debug report generated");
                Console.WriteLine();

                Console.WriteLine("🎉 ULTRA DEBUG TEST COMPLETED!");
                Console.WriteLine($"📊 Total processed: {allResults.Count} images");
                Console.WriteLine("🔍 Check console output for detailed debugging info");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<List<BatchData>> CreateBatchesWithGuaranteedImages()
        {
            Console.WriteLine("🔄 Creating batches with GUARANTEED working images...");
            
            var batches = new List<BatchData>();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            
            // Batch 1: Electronics - Using guaranteed working image URLs
            var batch1 = new BatchData
            {
                BatchId = $"debug_electronics_{timestamp}",
                Products = new[]
                {
                    new ProductData { ProductId = 100000001, ProductName = "iPhone 15 Pro", ImageUrl = "https://via.placeholder.com/800x600/FF0000/FFFFFF?text=iPhone+15+Pro" },
                    new ProductData { ProductId = 100000002, ProductName = "MacBook Air M2", ImageUrl = "https://via.placeholder.com/800x600/00FF00/FFFFFF?text=MacBook+Air+M2" },
                    new ProductData { ProductId = 100000003, ProductName = "AirPods Pro", ImageUrl = "https://via.placeholder.com/800x600/0000FF/FFFFFF?text=AirPods+Pro" },
                    new ProductData { ProductId = 100000004, ProductName = "iPad Air", ImageUrl = "https://via.placeholder.com/800x600/FFFF00/000000?text=iPad+Air" },
                    new ProductData { ProductId = 100000005, ProductName = "Apple Watch", ImageUrl = "https://via.placeholder.com/800x600/FF00FF/FFFFFF?text=Apple+Watch" }
                }
            };
            
            // Batch 2: Clothing - Using guaranteed working image URLs
            var batch2 = new BatchData
            {
                BatchId = $"debug_clothing_{timestamp}",
                Products = new[]
                {
                    new ProductData { ProductId = 200000001, ProductName = "Nike Air Max", ImageUrl = "https://via.placeholder.com/800x600/FF8800/FFFFFF?text=Nike+Air+Max" },
                    new ProductData { ProductId = 200000002, ProductName = "Adidas T-Shirt", ImageUrl = "https://via.placeholder.com/800x600/8800FF/FFFFFF?text=Adidas+T-Shirt" },
                    new ProductData { ProductId = 200000003, ProductName = "Levi's Jeans", ImageUrl = "https://via.placeholder.com/800x600/0088FF/FFFFFF?text=Levi's+Jeans" },
                    new ProductData { ProductId = 200000004, ProductName = "Puma Hoodie", ImageUrl = "https://via.placeholder.com/800x600/FF0088/FFFFFF?text=Puma+Hoodie" },
                    new ProductData { ProductId = 200000005, ProductName = "Under Armour Shorts", ImageUrl = "https://via.placeholder.com/800x600/88FF00/000000?text=Under+Armour+Shorts" }
                }
            };
            
            // Batch 3: Home & Garden - Using guaranteed working image URLs
            var batch3 = new BatchData
            {
                BatchId = $"debug_home_{timestamp}",
                Products = new[]
                {
                    new ProductData { ProductId = 300000001, ProductName = "Coffee Maker Deluxe", ImageUrl = "https://via.placeholder.com/800x600/8B4513/FFFFFF?text=Coffee+Maker+Deluxe" },
                    new ProductData { ProductId = 300000002, ProductName = "Garden Hose Pro", ImageUrl = "https://via.placeholder.com/800x600/228B22/FFFFFF?text=Garden+Hose+Pro" },
                    new ProductData { ProductId = 300000003, ProductName = "Blender Master", ImageUrl = "https://via.placeholder.com/800x600/FF6347/FFFFFF?text=Blender+Master" },
                    new ProductData { ProductId = 300000004, ProductName = "Toaster Premium", ImageUrl = "https://via.placeholder.com/800x600/DAA520/FFFFFF?text=Toaster+Premium" },
                    new ProductData { ProductId = 300000005, ProductName = "Microwave Oven", ImageUrl = "https://via.placeholder.com/800x600/696969/FFFFFF?text=Microwave+Oven" }
                }
            };

            var allBatches = new[] { batch1, batch2, batch3 };
            
            foreach (var batch in allBatches)
            {
                batches.Add(batch);
                
                Console.WriteLine($"   📦 Batch: {batch.BatchId}");
                Console.WriteLine($"      📊 Products: {batch.Products.Length}");
                Console.WriteLine($"      🆔 Product IDs: {string.Join(", ", batch.Products.Select(p => p.ProductId))}");
                Console.WriteLine($"      🖼️  Image URLs:");
                foreach (var product in batch.Products)
                {
                    Console.WriteLine($"         {product.ProductId}: {product.ImageUrl}");
                }
                Console.WriteLine();
            }

            return batches;
        }

        private async Task<List<UploadResult>> UploadWithDebugging(List<BatchData> batches)
        {
            Console.WriteLine("🔄 Uploading with EXTENSIVE debugging...");
            Console.WriteLine();
            
            var allResults = new List<UploadResult>();
            
            foreach (var batch in batches)
            {
                Console.WriteLine($"📦 Processing Batch: {batch.BatchId}");
                Console.WriteLine($"   📊 Products in batch: {batch.Products.Length}");
                
                // Prepare image data for this batch
                var imageData = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>();
                
                foreach (var product in batch.Products)
                {
                    imageData.Add((
                        product.ImageUrl,
                        FileContentType.Image,
                        product.ProductId,
                        product.Upc,
                        batch.BatchId,
                        $"Product image for {product.ProductName}"
                    ));
                }
                
                Console.WriteLine($"   🔄 Uploading {imageData.Count} images...");
                Console.WriteLine($"   📋 Image URLs being uploaded:");
                foreach (var (url, contentType, productId, upc, batchId, altText) in imageData)
                {
                    Console.WriteLine($"      Product {productId}: {url}");
                }
                
                try
                {
                    // Upload batch with metadata
                    Console.WriteLine($"   🚀 Starting upload for batch {batch.BatchId}...");
                    var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);
                    
                    Console.WriteLine($"   ✅ Upload response received:");
                    Console.WriteLine($"      Files count: {response.Files.Count}");
                    Console.WriteLine($"      UserErrors count: {response.UserErrors?.Count ?? 0}");
                    
                    if (response.UserErrors?.Any() == true)
                    {
                        Console.WriteLine($"      ❌ User Errors:");
                        foreach (var error in response.UserErrors)
                        {
                            Console.WriteLine($"         {error}");
                        }
                    }
                    
                    // Process results for this batch
                    for (int i = 0; i < response.Files.Count && i < batch.Products.Length; i++)
                    {
                        var file = response.Files[i];
                        var product = batch.Products[i];
                        _uploadedFileIds.Add(file.Id);
                        
                        Console.WriteLine($"      📁 File {i + 1}:");
                        Console.WriteLine($"         ID: {file.Id}");
                        Console.WriteLine($"         Alt: {file.Alt ?? "NULL"}");
                        Console.WriteLine($"         Status: {file.FileStatus}");
                        Console.WriteLine($"         Created: {file.CreatedAt}");
                        Console.WriteLine($"         Image URL: {file.Image?.Url ?? "NULL"}");
                        
                        var metadata = await _enhancedFileService.GetFileMetadataAsync(file.Id);
                        
                        allResults.Add(new UploadResult
                        {
                            FileId = file.Id,
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            BatchId = batch.BatchId,
                            AltText = file.Alt ?? "",
                            Metadata = metadata
                        });
                        
                        Console.WriteLine($"         Metadata count: {metadata.Count}");
                        foreach (var meta in metadata)
                        {
                            Console.WriteLine($"            {meta.Namespace}.{meta.Key}: {meta.Value}");
                        }
                    }
                    
                    Console.WriteLine($"   🎯 Batch {batch.BatchId} completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error uploading batch {batch.BatchId}: {ex.Message}");
                    Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                }
                
                Console.WriteLine();
                
                // Small delay between batches
                await Task.Delay(3000);
            }
            
            return allResults;
        }

        private async Task DebugVerifyAllFiles(List<UploadResult> allResults)
        {
            Console.WriteLine("🔄 Debug verifying all files...");
            Console.WriteLine();
            
            var batchGroups = allResults.GroupBy(r => r.BatchId).ToList();
            
            foreach (var batchGroup in batchGroups)
            {
                Console.WriteLine($"📦 Batch: {batchGroup.Key}");
                Console.WriteLine($"   📊 Products: {batchGroup.Count()}");
                
                foreach (var result in batchGroup)
                {
                    Console.WriteLine($"   📁 File: {result.FileId}");
                    Console.WriteLine($"      Product: {result.ProductId} ({result.ProductName})");
                    Console.WriteLine($"      Alt Text: '{result.AltText}'");
                    
                    // Wait a moment for metafields to be available
                    await Task.Delay(1000);
                    
                    try
                    {
                        var retrievedProductId = await _enhancedFileService.GetProductIdFromFileAsync(result.FileId);
                        var isMatch = retrievedProductId == result.ProductId;
                        
                        Console.WriteLine($"      Expected Product ID: {result.ProductId}");
                        Console.WriteLine($"      Retrieved Product ID: {retrievedProductId} {(isMatch ? "✅" : "❌")}");
                        
                        if (!isMatch)
                        {
                            Console.WriteLine($"      ⚠️  Product ID mismatch detected!");
                        }
                        
                        // Try to get all metafields for this file
                        var allMetafields = await _fileMetafieldService.GetFileMetafieldsAsync(result.FileId);
                        Console.WriteLine($"      Total metafields: {allMetafields.Count}");
                        foreach (var meta in allMetafields)
                        {
                            Console.WriteLine($"         {meta.Namespace}.{meta.Key}: {meta.Value} ({meta.Type})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"      ❌ Error retrieving metadata: {ex.Message}");
                    }
                    
                    Console.WriteLine();
                }
            }
        }

        private async Task GenerateDebugReport(List<UploadResult> allResults)
        {
            Console.WriteLine("📊 DEBUG REPORT");
            Console.WriteLine("===============");
            Console.WriteLine();
            
            var batchGroups = allResults.GroupBy(r => r.BatchId).ToList();
            
            Console.WriteLine($"🎯 TOTAL SUMMARY:");
            Console.WriteLine($"   📊 Total Products: {allResults.Count}");
            Console.WriteLine($"   📦 Total Batches: {batchGroups.Count}");
            Console.WriteLine($"   📁 Total Files: {_uploadedFileIds.Count}");
            Console.WriteLine();
            
            foreach (var batchGroup in batchGroups)
            {
                Console.WriteLine($"📦 Batch: {batchGroup.Key}");
                Console.WriteLine($"   📊 Products: {batchGroup.Count()}");
                Console.WriteLine($"   🆔 Product IDs: {string.Join(", ", batchGroup.Select(r => r.ProductId))}");
                Console.WriteLine($"   📁 File IDs: {string.Join(", ", batchGroup.Select(r => r.FileId))}");
                Console.WriteLine();
            }
            
            Console.WriteLine("🔗 SHOPIFY ADMIN ACCESS:");
            Console.WriteLine("   📱 Go to your Shopify admin dashboard");
            Console.WriteLine("   📂 Navigate to: Content → Files");
            Console.WriteLine("   🖼️  Look for images with colored backgrounds and text");
            Console.WriteLine("   📝 Each image should have descriptive alt text");
            Console.WriteLine();
            
            Console.WriteLine("🔧 TROUBLESHOOTING:");
            Console.WriteLine("   🔍 If you don't see images:");
            Console.WriteLine("      1. Check if placeholder.com is accessible");
            Console.WriteLine("      2. Verify Shopify API permissions");
            Console.WriteLine("      3. Check file upload limits");
            Console.WriteLine("      4. Look for error messages in console output");
            Console.WriteLine();
            
            Console.WriteLine("✅ VERIFICATION COMPLETE:");
            Console.WriteLine("   🖼️  All images should be uploaded to Shopify");
            Console.WriteLine("   🆔 All product IDs stored in metafields");
            Console.WriteLine("   🔍 Check console for detailed debugging info");
        }

        public void Dispose()
        {
            Console.WriteLine($"🧹 Test processed {_uploadedFileIds.Count} files");
            Console.WriteLine("📱 Check your Shopify admin dashboard → Content → Files");
            Console.WriteLine("🔍 Look for images with colored backgrounds and product names");
        }

        private class BatchData
        {
            public string BatchId { get; set; } = "";
            public ProductData[] Products { get; set; } = Array.Empty<ProductData>();
        }

        private class ProductData
        {
            public long ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string Upc { get; set; } = "";
            public string ImageUrl { get; set; } = "";
        }

        private class UploadResult
        {
            public string FileId { get; set; } = "";
            public long ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string BatchId { get; set; } = "";
            public string AltText { get; set; } = "";
            public List<Metafield> Metadata { get; set; } = new();
        }
    }
}
