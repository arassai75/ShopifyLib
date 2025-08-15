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
    /// Test to directly query a specific file GID and verify its product ID
    /// </summary>
    [IntegrationTest]
    public class DirectFileQueryTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;

        public DirectFileQueryTest()
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
        public async Task DirectFileQuery_ShouldFindProductIdForKnownFileGid()
        {
            Console.WriteLine("=== DIRECT FILE QUERY TEST ===");
            Console.WriteLine("🔍 Directly querying known file GID for product 300000005");
            Console.WriteLine();

            try
            {
                // The file GID we know from our previous upload test
                var knownFileGid = "gid://shopify/MediaImage/31573699297457";
                
                // Step 1: Query the specific file directly
                await QuerySpecificFile(knownFileGid);
                Console.WriteLine("✅ Step 1: Queried specific file");
                Console.WriteLine();

                // Step 2: Get product ID from the file
                await GetProductIdFromFile(knownFileGid);
                Console.WriteLine("✅ Step 2: Got product ID from file");
                Console.WriteLine();

                // Step 3: Get UPC from the file
                await GetUpcFromFile(knownFileGid);
                Console.WriteLine("✅ Step 3: Got UPC from file");
                Console.WriteLine();

                Console.WriteLine("🎉 DIRECT FILE QUERY TEST COMPLETED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task QuerySpecificFile(string fileGid)
        {
            Console.WriteLine($"🔄 Querying specific file: {fileGid}");
            
            try
            {
                // Get file metafields
                var metafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileGid);
                
                Console.WriteLine($"   📊 Found {metafields.Count} metafields");
                
                foreach (var meta in metafields)
                {
                    Console.WriteLine($"   📋 {meta.Namespace}.{meta.Key}: {meta.Value} ({meta.Type})");
                }
                
                // Get file details via GraphQL
                var query = @"
                    query getFile($id: ID!) {
                        node(id: $id) {
                            ... on MediaImage {
                                id
                                alt
                                fileStatus
                                createdAt
                                image {
                                    url
                                    width
                                    height
                                }
                                metafields(first: 10) {
                                    edges {
                                        node {
                                            id
                                            namespace
                                            key
                                            value
                                            type
                                        }
                                    }
                                }
                            }
                        }
                    }";
                
                var variables = new { id = fileGid };
                var response = await _client.GraphQL.ExecuteQueryAsync(query, variables);
                
                Console.WriteLine($"   📋 GraphQL Response:");
                Console.WriteLine($"      {response}");
                
                // Parse key information
                if (response.Contains("fileStatus"))
                {
                    var statusStart = response.IndexOf("\"fileStatus\":\"") + 14;
                    var statusEnd = response.IndexOf("\"", statusStart);
                    if (statusEnd > statusStart)
                    {
                        var status = response.Substring(statusStart, statusEnd - statusStart);
                        Console.WriteLine($"   📊 File Status: {status}");
                    }
                }
                
                if (response.Contains("\"alt\":"))
                {
                    var altStart = response.IndexOf("\"alt\":\"") + 7;
                    var altEnd = response.IndexOf("\"", altStart);
                    if (altEnd > altStart)
                    {
                        var alt = response.Substring(altStart, altEnd - altStart);
                        Console.WriteLine($"   📝 Alt Text: {alt}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error querying file: {ex.Message}");
            }
        }

        private async Task GetProductIdFromFile(string fileGid)
        {
            Console.WriteLine($"🆔 Getting product ID from file: {fileGid}");
            
            try
            {
                var productId = await _enhancedFileService.GetProductIdFromFileAsync(fileGid);
                Console.WriteLine($"   🎯 Product ID: {productId}");
                
                // Check if it matches what we expect
                var expectedProductId = 300000005L;
                var isMatch = productId == expectedProductId;
                Console.WriteLine($"   ✅ Matches expected {expectedProductId}: {isMatch}");
                
                if (isMatch)
                {
                    Console.WriteLine($"   🎉 SUCCESS! Found product {expectedProductId} in file {fileGid}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️  Product ID mismatch. Expected: {expectedProductId}, Found: {productId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error getting product ID: {ex.Message}");
            }
        }

        private async Task GetUpcFromFile(string fileGid)
        {
            Console.WriteLine($"📋 Getting UPC from file: {fileGid}");
            
            try
            {
                var upc = await _enhancedFileService.GetUpcFromFileAsync(fileGid);
                Console.WriteLine($"   🎯 UPC: {upc}");
                
                if (!string.IsNullOrEmpty(upc))
                {
                    Console.WriteLine($"   🎉 SUCCESS! Found UPC {upc} in file {fileGid}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️  No UPC found in file {fileGid}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error getting UPC: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Console.WriteLine("🧹 Direct file query test completed");
        }
    }
}
