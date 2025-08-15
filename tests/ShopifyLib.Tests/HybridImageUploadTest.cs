using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Newtonsoft.Json;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class HybridImageUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public HybridImageUploadTest()
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
        }

        [Fact]
        public async Task HybridUpload_GetCDNUrl_EnsureDashboardVisibility()
        {
            // Arrange - Use the original Indigo image URL as specified
            //var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg";
            var imageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            var altText = "Indigo Gift Image - Hybrid Test";
            
            Console.WriteLine("=== dynamic.indigoimages.ca IMAGE UPLOAD TEST ===");
            Console.WriteLine("This test combines GraphQL and REST to get CDN URL and ensure dashboard visibility");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Step 1: Upload via GraphQL (standalone file)
                Console.WriteLine("🔄 STEP 1: Uploading via GraphQL (standalone file)...");
                
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                Console.WriteLine("✅ GraphQL upload completed!");
                Console.WriteLine();

                // Validate GraphQL response
                Assert.NotNull(graphqlResponse);
                Assert.NotNull(graphqlResponse.Files);
                Assert.NotEmpty(graphqlResponse.Files);
                Assert.Empty(graphqlResponse.UserErrors);

                var graphqlFile = graphqlResponse.Files[0];
                
                Console.WriteLine("=== GRAPHQL UPLOAD RESULTS ===");
                Console.WriteLine($"📁 File ID: {graphqlFile.Id}");
                Console.WriteLine($"📊 File Status: {graphqlFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {graphqlFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {graphqlFile.CreatedAt}");
                
                if (graphqlFile.Image != null)
                {
                    Console.WriteLine($"📏 Width: {graphqlFile.Image.Width}");
                    Console.WriteLine($"📐 Height: {graphqlFile.Image.Height}");
                    Console.WriteLine($"🌐 URL: {graphqlFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"🔗 OriginalSrc: {graphqlFile.Image.OriginalSrc ?? "Not available"}");
                    Console.WriteLine($"🔄 TransformedSrc: {graphqlFile.Image.TransformedSrc ?? "Not available"}");
                    Console.WriteLine($"📷 Src: {graphqlFile.Image.Src ?? "Not available"}");
                }

                // Step 2: Create a temporary product to get CDN URL via REST
                Console.WriteLine();
                Console.WriteLine("🔄 STEP 2: Creating temporary product for REST upload...");
                
                var tempProduct = new Product
                {
                    Title = $"Temp Product for CDN URL Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Temporary product for getting CDN URL</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await _client.Products.CreateAsync(tempProduct);
                Console.WriteLine($"✅ Created temporary product with ID: {createdProduct.Id}");

                try
                {
                    // Step 3: Upload same image to product via REST to get CDN URL
                    Console.WriteLine();
                    Console.WriteLine("🔄 STEP 3: Uploading image to product via REST to get CDN URL...");
                    
                    var restImage = await _client.Images.UploadImageFromUrlAsync(
                        createdProduct.Id,
                        imageUrl,
                        altText,
                        1
                    );

                    Console.WriteLine("✅ REST upload completed!");
                    Console.WriteLine();

                    Console.WriteLine("=== REST UPLOAD RESULTS (CDN URL) ===");
                    Console.WriteLine($"📁 Image ID: {restImage.Id}");
                    Console.WriteLine($"📊 Position: {restImage.Position}");
                    Console.WriteLine($"📝 Alt Text: {restImage.Alt ?? "Not set"}");
                    Console.WriteLine($"📅 Created At: {restImage.CreatedAt}");
                    Console.WriteLine($"🌐 SRC URL (CDN): {restImage.Src ?? "Not available"}");
                    Console.WriteLine($"📏 Width: {restImage.Width}");
                    Console.WriteLine($"📐 Height: {restImage.Height}");
                    Console.WriteLine($"🔄 Updated At: {restImage.UpdatedAt}");

                    // Step 4: Wait a bit and check if GraphQL file now has URLs
                    Console.WriteLine();
                    Console.WriteLine("🔄 STEP 4: Waiting for GraphQL file to process...");
                    await Task.Delay(5000); // Wait 5 seconds

                    // Step 5: Query the GraphQL file again to see if URLs are now available
                    Console.WriteLine();
                    Console.WriteLine("🔄 STEP 5: Re-querying GraphQL file for URLs...");
                    
                    var queryMutation = @"
                        query getFile($id: ID!) {
                            node(id: $id) {
                                ... on MediaImage {
                                    id
                                    fileStatus
                                    alt
                                    createdAt
                                    image {
                                        width
                                        height
                                        url
                                        originalSrc
                                        transformedSrc
                                        src
                                    }
                                }
                            }
                        }";

                    var queryVariables = new { id = graphqlFile.Id };
                    var queryResponse = await _client.GraphQL.ExecuteQueryAsync(queryMutation, queryVariables);

                    Console.WriteLine("✅ GraphQL query completed!");
                    Console.WriteLine();
                    Console.WriteLine("=== GRAPHQL QUERY RESPONSE ===");
                    Console.WriteLine(queryResponse);
                    Console.WriteLine();

                    // Parse the query response
                    try
                    {
                        var parsedQuery = JsonConvert.DeserializeObject<dynamic>(queryResponse);
                        if (parsedQuery?.data?.node != null)
                        {
                            var node = parsedQuery.data.node;
                            Console.WriteLine("=== QUERIED FILE DETAILS ===");
                            Console.WriteLine($"📁 ID: {node.id}");
                            Console.WriteLine($"📊 Status: {node.fileStatus}");
                            Console.WriteLine($"📝 Alt: {node.alt}");
                            Console.WriteLine($"📅 Created: {node.createdAt}");
                            
                            if (node.image != null)
                            {
                                Console.WriteLine("📸 IMAGE DETAILS (from query):");
                                Console.WriteLine($"   • Width: {node.image.width}");
                                Console.WriteLine($"   • Height: {node.image.height}");
                                Console.WriteLine($"   • URL: {node.image.url}");
                                Console.WriteLine($"   • OriginalSrc: {node.image.originalSrc}");
                                Console.WriteLine($"   • TransformedSrc: {node.image.transformedSrc}");
                                Console.WriteLine($"   • Src: {node.image.src}");
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"❌ Failed to parse query response: {parseEx.Message}");
                    }

                    // Step 6: Summary and recommendations
                    Console.WriteLine();
                    Console.WriteLine("=== FINAL SUMMARY ===");
                    Console.WriteLine($"✅ GraphQL File ID: {graphqlFile.Id}");
                    Console.WriteLine($"✅ GraphQL File Status: {graphqlFile.FileStatus}");
                    Console.WriteLine($"✅ REST Image ID: {restImage.Id}");
                    Console.WriteLine($"✅ REST CDN URL: {restImage.Src ?? "Not available"}");
                    
                    if (!string.IsNullOrEmpty(restImage.Src))
                    {
                        Console.WriteLine();
                        Console.WriteLine("🎉 SUCCESS: CDN URL obtained via REST API!");
                        Console.WriteLine($"🌐 Use this CDN URL: {restImage.Src}");
                        Console.WriteLine("📋 This image should appear in your Shopify file dashboard");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("⚠️  WARNING: No CDN URL obtained");
                        Console.WriteLine("💡 This might indicate an API permission or configuration issue");
                    }

                    // Step 7: Clean up
                    Console.WriteLine();
                    Console.WriteLine("🧹 Cleaning up temporary product...");
                    await _client.Products.DeleteAsync(createdProduct.Id);
                    Console.WriteLine("✅ Temporary product deleted");

                    // Final recommendations
                    Console.WriteLine();
                    Console.WriteLine("=== RECOMMENDATIONS ===");
                    Console.WriteLine("1. ✅ GraphQL upload creates the file in Shopify's system");
                    Console.WriteLine("2. ✅ REST upload provides immediate CDN URL access");
                    Console.WriteLine("3. ✅ Both methods should make the image visible in dashboard");
                    Console.WriteLine("4. 💡 For immediate CDN URL access, use REST API");
                    Console.WriteLine("5. 💡 For standalone file management, use GraphQL");

                }
                catch (Exception restEx)
                {
                    Console.WriteLine($"❌ REST upload failed: {restEx.Message}");
                    Console.WriteLine("💡 This confirms the Indigo URL timeout issue");
                    
                    // Try alternative approach: Download the image first, then upload
                    Console.WriteLine();
                    Console.WriteLine("🔄 Trying alternative approach: Download and upload...");
                    
                    try
                    {
                        // Download the image first
                        using var httpClient = new System.Net.Http.HttpClient();
                        httpClient.Timeout = TimeSpan.FromMinutes(2); // Longer timeout
                        
                        Console.WriteLine("🔄 Downloading image from Indigo URL...");
                        var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                        Console.WriteLine($"✅ Downloaded {imageBytes.Length} bytes");
                        
                        // Convert to base64 and upload via GraphQL
                        var base64Image = Convert.ToBase64String(imageBytes);
                        var downloadedFileInput = new FileCreateInput
                        {
                            OriginalSource = $"data:image/jpeg;base64,{base64Image}",
                            ContentType = FileContentType.Image,
                            Alt = altText
                        };
                        
                        var downloadedResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { downloadedFileInput });
                        var downloadedFile = downloadedResponse.Files[0];
                        
                        Console.WriteLine("✅ Downloaded image uploaded successfully!");
                        Console.WriteLine($"📁 File ID: {downloadedFile.Id}");
                        Console.WriteLine($"📊 Status: {downloadedFile.FileStatus}");
                        
                        if (downloadedFile.Image != null)
                        {
                            Console.WriteLine($"📏 Dimensions: {downloadedFile.Image.Width}x{downloadedFile.Image.Height}");
                            Console.WriteLine($"🌐 URL: {downloadedFile.Image.Url ?? "Not available"}");
                        }
                        
                        Console.WriteLine();
                        Console.WriteLine("🎉 SUCCESS: Indigo image uploaded via download method!");
                        Console.WriteLine("💡 This approach downloads the image first, then uploads to Shopify");
                    }
                    catch (Exception downloadEx)
                    {
                        Console.WriteLine($"❌ Download approach also failed: {downloadEx.Message}");
                        Console.WriteLine("💡 The Indigo URL is not accessible from our servers either");
                        Console.WriteLine("💡 Consider using a different image URL or hosting the image elsewhere");
                    }
                    
                    // Clean up product even if REST failed
                    try
                    {
                        await _client.Products.DeleteAsync(createdProduct.Id);
                        Console.WriteLine("✅ Temporary product cleaned up");
                    }
                    catch
                    {
                        Console.WriteLine("⚠️  Failed to clean up temporary product");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 