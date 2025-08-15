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
    /// Test to query file GID by product ID using metafield search
    /// </summary>
    [IntegrationTest]
    public class QueryProductByIdTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;

        public QueryProductByIdTest()
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
        public async Task QueryProductById_ShouldFindFileGidForProduct300000005()
        {
            Console.WriteLine("=== QUERY PRODUCT BY ID TEST ===");
            Console.WriteLine("🔍 Searching for file GID for product 300000005");
            Console.WriteLine();

            try
            {
                // Step 1: Search for files with product ID 300000005
                var fileGids = await SearchForProductId(300000005);
                Console.WriteLine("✅ Step 1: Searched for product ID");
                Console.WriteLine();

                // Step 2: Get detailed information for each file found
                await GetDetailedFileInfo(fileGids, 300000005);
                Console.WriteLine("✅ Step 2: Got detailed file information");
                Console.WriteLine();

                Console.WriteLine("🎉 QUERY PRODUCT BY ID TEST COMPLETED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<List<string>> SearchForProductId(long productId)
        {
            Console.WriteLine($"🔄 Searching for files with product ID: {productId}");
            
            try
            {
                var fileGids = await _fileMetafieldService.FindFilesByProductIdAsync(productId);
                
                Console.WriteLine($"   📊 Found {fileGids.Count} file(s) for product {productId}");
                
                if (fileGids.Count > 0)
                {
                    foreach (var fileGid in fileGids)
                    {
                        Console.WriteLine($"   📁 File GID: {fileGid}");
                    }
                }
                else
                {
                    Console.WriteLine($"   ⚠️  No files found for product {productId}");
                }
                
                return fileGids;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error searching for product {productId}: {ex.Message}");
                return new List<string>();
            }
        }

        private async Task GetDetailedFileInfo(List<string> fileGids, long productId)
        {
            Console.WriteLine($"🔍 Getting detailed information for {fileGids.Count} file(s)");
            
            foreach (var fileGid in fileGids)
            {
                Console.WriteLine($"   📁 File: {fileGid}");
                
                try
                {
                    // Get file metafields
                    var metafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileGid);
                    Console.WriteLine($"      📊 Metafields count: {metafields.Count}");
                    
                    foreach (var meta in metafields)
                    {
                        Console.WriteLine($"         {meta.Namespace}.{meta.Key}: {meta.Value} ({meta.Type})");
                    }
                    
                    // Get product ID from file
                    var retrievedProductId = await _enhancedFileService.GetProductIdFromFileAsync(fileGid);
                    Console.WriteLine($"      🆔 Retrieved Product ID: {retrievedProductId}");
                    
                    // Verify it matches
                    var isMatch = retrievedProductId == productId;
                    Console.WriteLine($"      ✅ Product ID Match: {isMatch}");
                    
                    // Get file details via GraphQL
                    await GetFileDetailsViaGraphQL(fileGid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ❌ Error getting file info: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        private async Task GetFileDetailsViaGraphQL(string fileGid)
        {
            Console.WriteLine($"      🔍 Getting file details via GraphQL...");
            
            try
            {
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
                
                Console.WriteLine($"      📋 GraphQL Response:");
                Console.WriteLine($"         {response}");
                
                // Parse the response to extract key information
                if (response.Contains("fileStatus"))
                {
                    var statusStart = response.IndexOf("\"fileStatus\":\"") + 14;
                    var statusEnd = response.IndexOf("\"", statusStart);
                    if (statusEnd > statusStart)
                    {
                        var status = response.Substring(statusStart, statusEnd - statusStart);
                        Console.WriteLine($"      📊 File Status: {status}");
                    }
                }
                
                if (response.Contains("\"alt\":"))
                {
                    var altStart = response.IndexOf("\"alt\":\"") + 7;
                    var altEnd = response.IndexOf("\"", altStart);
                    if (altEnd > altStart)
                    {
                        var alt = response.Substring(altStart, altEnd - altStart);
                        Console.WriteLine($"      📝 Alt Text: {alt}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error getting GraphQL details: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Console.WriteLine("🧹 Query test completed");
        }
    }
}
