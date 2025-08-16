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
    /// Test to debug image URL issues and wait for processing
    /// </summary>
    [IntegrationTest]
    public class ImageUrlDebugTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly FileMetafieldService _fileMetafieldService;
        private readonly EnhancedFileServiceWithMetadata _enhancedFileService;
        private readonly List<string> _uploadedFileIds = new List<string>();

        public ImageUrlDebugTest()
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
        public async Task ImageUrlDebug_UploadAndWaitForProcessing_ShouldShowActualUrls()
        {
            Console.WriteLine("=== IMAGE URL DEBUG TEST ===");
            Console.WriteLine("🔍 Debugging why image URLs are NULL");
            Console.WriteLine("⏳ Waiting for images to be fully processed");
            Console.WriteLine();

            try
            {
                // Step 1: Upload a single test image
                var testImage = await UploadSingleTestImage();
                Console.WriteLine("✅ Step 1: Uploaded test image");
                Console.WriteLine();

                // Step 2: Wait and check image processing
                await WaitAndCheckImageProcessing(testImage);
                Console.WriteLine("✅ Step 2: Checked image processing");
                Console.WriteLine();

                // Step 3: Get detailed file info
                await GetDetailedFileInfo(testImage);
                Console.WriteLine("✅ Step 3: Got detailed file info");
                Console.WriteLine();

                Console.WriteLine("🎉 IMAGE URL DEBUG TEST COMPLETED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<string> UploadSingleTestImage()
        {
            Console.WriteLine("🔄 Uploading single test image...");
            
            var imageData = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>
            {
                (
                    "https://via.placeholder.com/800x600/FF0000/FFFFFF?text=DEBUG+TEST",
                    FileContentType.Image,
                    999999999,
                    "123456789012",
                    "debug_test_batch",
                    "Debug test image"
                )
            };
            
            Console.WriteLine($"   📋 Uploading: {imageData[0].ImageUrl}");
            
            var response = await _enhancedFileService.UploadImagesWithMetadataAsync(imageData);
            
            Console.WriteLine($"   ✅ Upload response:");
            Console.WriteLine($"      Files count: {response.Files.Count}");
            Console.WriteLine($"      UserErrors count: {response.UserErrors?.Count ?? 0}");
            
            if (response.Files.Count > 0)
            {
                var file = response.Files[0];
                _uploadedFileIds.Add(file.Id);
                
                Console.WriteLine($"      📁 File ID: {file.Id}");
                Console.WriteLine($"      📝 Alt: {file.Alt ?? "NULL"}");
                Console.WriteLine($"      📊 Status: {file.FileStatus}");
                Console.WriteLine($"      🕐 Created: {file.CreatedAt}");
                Console.WriteLine($"      🖼️  Image URL: {file.Image?.Url ?? "NULL"}");
                Console.WriteLine($"      🔗 Original Source: {file.Image?.OriginalSrc ?? "NULL"}");
                
                return file.Id;
            }
            
            throw new Exception("No files were uploaded");
        }

        private async Task WaitAndCheckImageProcessing(string fileId)
        {
            Console.WriteLine("⏳ Waiting for image processing...");
            
            for (int i = 1; i <= 10; i++)
            {
                Console.WriteLine($"   🔄 Check {i}/10 - Waiting 5 seconds...");
                await Task.Delay(5000);
                
                try
                {
                    // Try to get the file details again
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
                                    preview {
                                        image {
                                            url
                                            width
                                            height
                                        }
                                    }
                                }
                            }
                        }";
                    
                    var variables = new { id = fileId };
                    var response = await _client.GraphQL.ExecuteQueryAsync(query, variables);
                    
                    Console.WriteLine($"   📊 Raw GraphQL Response: {response}");
                    
                    if (response.Contains("image") && response.Contains("url"))
                    {
                        Console.WriteLine($"   ✅ Found image URL in response!");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"   ⏳ Still processing... (attempt {i})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error checking file: {ex.Message}");
                }
            }
        }

        private async Task GetDetailedFileInfo(string fileId)
        {
            Console.WriteLine("🔍 Getting detailed file information...");
            
            try
            {
                // Get file metafields
                var metafields = await _fileMetafieldService.GetFileMetafieldsAsync(fileId);
                Console.WriteLine($"   📊 Metafields count: {metafields.Count}");
                foreach (var meta in metafields)
                {
                    Console.WriteLine($"      {meta.Namespace}.{meta.Key}: {meta.Value} ({meta.Type})");
                }
                
                // Try to get product ID
                var productId = await _enhancedFileService.GetProductIdFromFileAsync(fileId);
                Console.WriteLine($"   🆔 Retrieved Product ID: {productId}");
                
                // Get file details with full GraphQL query
                var detailedQuery = @"
                    query getDetailedFile($id: ID!) {
                        node(id: $id) {
                            ... on MediaImage {
                                id
                                alt
                                fileStatus
                                createdAt
                                updatedAt
                                image {
                                    url
                                    width
                                    height
                                    originalSource {
                                        url
                                        width
                                        height
                                    }
                                }
                                preview {
                                    image {
                                        url
                                        width
                                        height
                                    }
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
                
                var variables = new { id = fileId };
                var response = await _client.GraphQL.ExecuteQueryAsync(detailedQuery, variables);
                
                Console.WriteLine($"   📋 Detailed GraphQL Response:");
                Console.WriteLine($"      {response}");
                
                // Parse the response to extract URLs
                if (response.Contains("image"))
                {
                    Console.WriteLine("   🎯 Found image data in response!");
                    
                    // Try to extract URLs manually
                    if (response.Contains("\"url\":"))
                    {
                        var urlStart = response.IndexOf("\"url\":") + 6;
                        var urlEnd = response.IndexOf("\"", urlStart + 1);
                        if (urlEnd > urlStart)
                        {
                            var url = response.Substring(urlStart + 1, urlEnd - urlStart - 1);
                            Console.WriteLine($"   🔗 Extracted URL: {url}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   ❌ No image data found in response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error getting detailed info: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"🧹 Test processed {_uploadedFileIds.Count} files");
            Console.WriteLine("📱 Check your Shopify admin dashboard → Content → Files");
            Console.WriteLine("🔍 Look for the debug test image");
        }
    }
}
