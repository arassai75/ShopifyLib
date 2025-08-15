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
    /// Debug test to understand metafield retrieval issues and batch upload problems
    /// </summary>
    [IntegrationTest]
    public class DebugMetafieldIssueTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public DebugMetafieldIssueTest()
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
        public async Task DebugMetafieldRetrieval_ShouldIdentifyTheRootCause()
        {
            Console.WriteLine("=== DEBUG METAFIELD RETRIEVAL ISSUE ===");
            Console.WriteLine("Testing single image upload and metafield storage/retrieval");
            Console.WriteLine();

            try
            {
                // Step 1: Upload a single image
                Console.WriteLine("🔄 Step 1: Uploading single image...");
                var imageData = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>
                {
                    ("https://httpbin.org/image/jpeg", FileContentType.Image, 999999999, "123456789012", "debug_batch_001", "Debug test image")
                };

                var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);
                
                if (response?.Files == null || response.Files.Count == 0)
                {
                    Console.WriteLine("❌ No files uploaded!");
                    return;
                }

                var fileId = response.Files[0].Id;
                _uploadedFileIds.Add(fileId);
                
                Console.WriteLine($"✅ Image uploaded: {fileId}");
                Console.WriteLine($"   Alt text: {response.Files[0].Alt ?? "None"}");
                Console.WriteLine();

                // Step 2: Wait a moment for metafields to be available
                Console.WriteLine("⏳ Step 2: Waiting for metafields to be available...");
                await Task.Delay(3000);
                Console.WriteLine();

                // Step 3: Try to retrieve metafields directly
                Console.WriteLine("🔍 Step 3: Testing direct metafield retrieval...");
                var metafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileId);
                Console.WriteLine($"   Found {metafields.Count} metafields directly");
                
                foreach (var metafield in metafields)
                {
                    Console.WriteLine($"   - {metafield.Namespace}.{metafield.Key}: {metafield.Value} ({metafield.Type})");
                }
                Console.WriteLine();

                // Step 4: Try to manually create a metafield
                Console.WriteLine("🔧 Step 4: Manually creating a test metafield...");
                try
                {
                    var testMetafield = await _fileMetafieldService.CreateOrUpdateFileMetafieldAsync(
                        fileId,
                        new MetafieldInput
                        {
                            Namespace = "debug",
                            Key = "test_key",
                            Value = "test_value",
                            Type = "single_line_text_field"
                        }
                    );
                    
                    Console.WriteLine($"✅ Test metafield created: {testMetafield.Id}");
                    Console.WriteLine($"   Namespace: {testMetafield.Namespace}");
                    Console.WriteLine($"   Key: {testMetafield.Key}");
                    Console.WriteLine($"   Value: {testMetafield.Value}");
                    Console.WriteLine($"   Type: {testMetafield.Type}");
                    Console.WriteLine();

                    // Step 5: Try to retrieve the test metafield
                    Console.WriteLine("🔍 Step 5: Retrieving the test metafield...");
                    await Task.Delay(2000);
                    var retrievedMetafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileId);
                    Console.WriteLine($"   Found {retrievedMetafields.Count} metafields after manual creation");
                    
                    foreach (var metafield in retrievedMetafields)
                    {
                        Console.WriteLine($"   - {metafield.Namespace}.{metafield.Key}: {metafield.Value} ({metafield.Type})");
                    }
                    Console.WriteLine();

                    // Step 6: Test the product ID retrieval method
                    Console.WriteLine("🔍 Step 6: Testing product ID retrieval method...");
                    var productId = await _fileMetafieldService.GetProductIdFromFileAsync(fileId);
                    Console.WriteLine($"   Retrieved product ID: {productId}");
                    Console.WriteLine();

                    // Step 7: Test the enhanced service method
                    Console.WriteLine("🔍 Step 7: Testing enhanced service method...");
                    var enhancedProductId = await _enhancedFileService.GetProductIdFromFileAsync(fileId);
                    Console.WriteLine($"   Enhanced service product ID: {enhancedProductId}");
                    Console.WriteLine();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Manual metafield creation failed: {ex.Message}");
                    Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                    Console.WriteLine();
                }

                // Step 8: Test GraphQL query directly
                Console.WriteLine("🔍 Step 8: Testing GraphQL query structure...");
                await TestGraphQLQueryStructure(fileId);
                Console.WriteLine();

                Console.WriteLine("🎯 DEBUG ANALYSIS COMPLETE");
                Console.WriteLine("Check the output above to identify the root cause");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Debug test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task TestGraphQLQueryStructure(string fileId)
        {
            try
            {
                // Test the exact GraphQL query that should work
                var query = @"
                    query GetFileMetafields($fileId: ID!) {
                        node(id: $fileId) {
                            ... on MediaImage {
                                id
                                metafields(first: 50) {
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

                var variables = new { fileId };
                var response = await _client.GraphQL.ExecuteQueryAsync(query, variables);
                
                Console.WriteLine($"   GraphQL Response: {response}");
                Console.WriteLine();
                
                // Parse the response
                var parsedResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                var node = parsedResponse?.data?.node;
                
                if (node != null)
                {
                    Console.WriteLine($"   Node ID: {node.id}");
                    Console.WriteLine($"   Node type: {node.GetType().Name}");
                    
                    if (node.metafields != null)
                    {
                        Console.WriteLine($"   Metafields found: {node.metafields.edges?.Count ?? 0}");
                        
                        if (node.metafields.edges != null)
                        {
                            foreach (var edge in node.metafields.edges)
                            {
                                var metafieldNode = edge.node;
                                Console.WriteLine($"   - {metafieldNode.@namespace}.{metafieldNode.key}: {metafieldNode.value}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("   No metafields property found on node");
                    }
                }
                else
                {
                    Console.WriteLine("   No node found in response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ GraphQL query failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"🧹 Debug test uploaded {_uploadedFileIds.Count} files to Shopify");
            Console.WriteLine("📱 Check your Shopify admin dashboard to see the debug image");
        }
    }
}
