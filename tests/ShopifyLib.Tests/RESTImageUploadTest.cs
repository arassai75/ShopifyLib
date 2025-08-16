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
    public class RESTImageUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public RESTImageUploadTest()
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
        public async Task UploadImageViaREST_CompareWithGraphQL_GetCDNUrl()
        {
            // Arrange - Use the specified  image URL
            var imageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = " Gift Image - REST Test";
            
            Console.WriteLine("=== REST vs GRAPHQL IMAGE UPLOAD COMPARISON ===");
            Console.WriteLine("This test compares REST API upload with GraphQL upload");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // First, let's try to create a temporary product to attach the image to
                Console.WriteLine("🔄 Creating temporary product for image attachment...");
                
                var tempProduct = new Product
                {
                    Title = $"Temp Product for Image Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Temporary product for testing image upload</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await _client.Products.CreateAsync(tempProduct);
                Console.WriteLine($"✅ Created temporary product with ID: {createdProduct.Id}");

                try
                {
                    // Method 1: Upload via REST API (attaching to product)
                    Console.WriteLine();
                    Console.WriteLine("=== METHOD 1: REST API UPLOAD ===");
                    Console.WriteLine("🔄 Uploading image via REST API...");
                    
                    var restImage = await _client.Images.UploadImageFromUrlAsync(
                        createdProduct.Id,
                        imageUrl,
                        altText,
                        1
                    );

                    Console.WriteLine("✅ REST API upload completed!");
                    Console.WriteLine();
                    Console.WriteLine("=== REST API RESPONSE ===");
                    Console.WriteLine(JsonConvert.SerializeObject(restImage, Formatting.Indented));
                    Console.WriteLine();

                    Console.WriteLine("=== REST API IMAGE DETAILS ===");
                    Console.WriteLine($"📁 Image ID: {restImage.Id}");
                    Console.WriteLine($"📊 Position: {restImage.Position}");
                    Console.WriteLine($"📝 Alt Text: {restImage.Alt ?? "Not set"}");
                    Console.WriteLine($"📅 Created At: {restImage.CreatedAt}");
                    Console.WriteLine($"🌐 SRC URL: {restImage.Src ?? "Not available"}");
                    Console.WriteLine($"📏 Width: {restImage.Width}");
                    Console.WriteLine($"📐 Height: {restImage.Height}");
                    Console.WriteLine($"🔄 Updated At: {restImage.UpdatedAt}");

                    // Method 2: Upload via GraphQL (standalone)
                    Console.WriteLine();
                    Console.WriteLine("=== METHOD 2: GRAPHQL UPLOAD ===");
                    Console.WriteLine("🔄 Uploading image via GraphQL...");
                    
                    var fileInput = new FileCreateInput
                    {
                        OriginalSource = imageUrl,
                        ContentType = FileContentType.Image,
                        Alt = altText
                    };

                    var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                    Console.WriteLine("✅ GraphQL upload completed!");
                    Console.WriteLine();
                    Console.WriteLine("=== GRAPHQL RESPONSE ===");
                    Console.WriteLine(JsonConvert.SerializeObject(graphqlResponse, Formatting.Indented));
                    Console.WriteLine();

                    var graphqlFile = graphqlResponse.Files[0];
                    Console.WriteLine("=== GRAPHQL FILE DETAILS ===");
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

                    // Comparison
                    Console.WriteLine();
                    Console.WriteLine("=== COMPARISON SUMMARY ===");
                    Console.WriteLine("REST API (Product Image):");
                    Console.WriteLine($"  ✅ Has SRC URL: {!string.IsNullOrEmpty(restImage.Src)}");
                    Console.WriteLine($"  ✅ SRC URL: {restImage.Src ?? "Not available"}");
                    Console.WriteLine($"  ✅ Has dimensions: {restImage.Width > 0 && restImage.Height > 0}");
                    Console.WriteLine($"  ✅ Image ID: {restImage.Id}");
                    
                    Console.WriteLine();
                    Console.WriteLine("GraphQL (Standalone File):");
                    Console.WriteLine($"  ✅ Has any URL: {graphqlFile.Image?.Url != null || graphqlFile.Image?.Src != null}");
                    Console.WriteLine($"  ✅ File Status: {graphqlFile.FileStatus}");
                    Console.WriteLine($"  ✅ Has dimensions: {graphqlFile.Image?.Width > 0 && graphqlFile.Image?.Height > 0}");
                    Console.WriteLine($"  ✅ File ID: {graphqlFile.Id}");

                    Console.WriteLine();
                    Console.WriteLine("=== RECOMMENDATIONS ===");
                    if (!string.IsNullOrEmpty(restImage.Src))
                    {
                        Console.WriteLine("✅ REST API provides the CDN URL immediately!");
                        Console.WriteLine($"🌐 Use this URL: {restImage.Src}");
                    }
                    else
                    {
                        Console.WriteLine("❌ REST API also doesn't provide CDN URL");
                    }

                    if (graphqlFile.Image?.Url != null || graphqlFile.Image?.Src != null)
                    {
                        Console.WriteLine("✅ GraphQL provides URLs!");
                        Console.WriteLine($"🌐 GraphQL URL: {graphqlFile.Image?.Url ?? graphqlFile.Image?.Src}");
                    }
                    else
                    {
                        Console.WriteLine("❌ GraphQL doesn't provide URLs either");
                        Console.WriteLine("💡 This might be a Shopify API limitation or configuration issue");
                    }

                }
                finally
                {
                    // Clean up - delete the temporary product
                    Console.WriteLine();
                    Console.WriteLine("🧹 Cleaning up temporary product...");
                    await _client.Products.DeleteAsync(createdProduct.Id);
                    Console.WriteLine("✅ Temporary product deleted");
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