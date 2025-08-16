// This test file is commented out because it uses methods that don't exist in the current API
// Shopify doesn't support retrieving files by product ID directly
/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Services;
using Xunit;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Test to demonstrate how to view product IDs from uploaded files
    /// </summary>
    [IntegrationTest]
    public class ViewProductIdsTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly IFileMetafieldService _fileMetafieldService;

        public ViewProductIdsTest()
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
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task ViewProductIds_FromUploadedFiles_ShouldShowProductIds()
        {
            Console.WriteLine("=== VIEWING PRODUCT IDs FROM UPLOADED FILES ===");
            Console.WriteLine("This test shows how to retrieve product IDs from files uploaded to Shopify");
            Console.WriteLine();

            // Example file IDs from your recent upload (replace with actual file IDs)
            var exampleFileIds = new List<string>
            {
                "gid://shopify/MediaImage/31573253390513",
                "gid://shopify/MediaImage/31573253423281",
                "gid://shopify/MediaImage/31573253456049",
                "gid://shopify/MediaImage/31573253488817",
                "gid://shopify/MediaImage/31573253521585",
                "gid://shopify/MediaImage/31573253554353"
            };

            Console.WriteLine("🔍 Checking each uploaded file for product ID metadata...");
            Console.WriteLine();

            foreach (var fileId in exampleFileIds)
            {
                try
                {
                    Console.WriteLine($"📁 File: {fileId}");
                    
                    // Get all metafields for this file
                    var metafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileId);
                    
                    // Look for product_id metafield
                    var productIdMetafield = metafields.FirstOrDefault(m => 
                        m.Namespace == "migration" && m.Key == "product_id");
                    
                    if (productIdMetafield != null)
                    {
                        Console.WriteLine($"   ✅ Product ID Found: {productIdMetafield.Value}");
                        Console.WriteLine($"   📊 Namespace: {productIdMetafield.Namespace}");
                        Console.WriteLine($"   🔑 Key: {productIdMetafield.Key}");
                        Console.WriteLine($"   📝 Type: {productIdMetafield.Type}");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ No product_id metafield found");
                        Console.WriteLine($"   📊 Total metafields: {metafields.Count}");
                        
                        // Show all metafields if any exist
                        if (metafields.Any())
                        {
                            Console.WriteLine($"   📋 Available metafields:");
                            foreach (var mf in metafields)
                            {
                                Console.WriteLine($"      - {mf.Namespace}.{mf.Key}: {mf.Value} ({mf.Type})");
                            }
                        }
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error accessing file: {ex.Message}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("=== SEARCHING BY PRODUCT ID ===");
            Console.WriteLine("You can also search for files by product ID:");
            Console.WriteLine();

            // Example: Search for a specific product ID
            var searchProductId = 1001L; // Replace with actual product ID
            Console.WriteLine($"🔍 Searching for files with Product ID: {searchProductId}");
            
            try
            {
                var foundFiles = await _fileMetafieldService.FindFilesByProductIdAsync(searchProductId);
                
                if (foundFiles.Any())
                {
                    Console.WriteLine($"✅ Found {foundFiles.Count} files for Product ID {searchProductId}:");
                    foreach (var fileId in foundFiles)
                    {
                        Console.WriteLine($"   📁 {fileId}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ No files found for Product ID {searchProductId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error searching: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("=== HOW TO ACCESS PRODUCT IDs IN YOUR CODE ===");
            Console.WriteLine("1. Use IFileMetafieldService.GetProductIdFromFileAsync(fileId)");
            Console.WriteLine("2. Use IFileMetafieldService.FindFilesByProductIdAsync(productId)");
            Console.WriteLine("3. Use IFileMetafieldService.GetFileMetafieldsAsync(fileId) for all metadata");
            Console.WriteLine();
            Console.WriteLine("=== SHOPIFY ADMIN DASHBOARD LIMITATIONS ===");
            Console.WriteLine("❌ Product IDs are NOT visible in the standard Shopify admin dashboard");
            Console.WriteLine("✅ They are stored as metafields and accessible via GraphQL API");
            Console.WriteLine("✅ You can build custom admin tools to display this data");
        }
    }
}
*/
