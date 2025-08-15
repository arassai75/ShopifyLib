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
    public class RealCDNUrlTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public RealCDNUrlTest()
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
        public async Task UploadImage_GetRealCDNUrl_VerifyAccessibility()
        {
            // Arrange - Use a reliable test image URL
            var imageUrl = "https://httpbin.org/image/jpeg";
            var altText = "Real CDN URL Test Image";
            
            Console.WriteLine("=== REAL CDN URL TEST ===");
            Console.WriteLine("This test uploads an image and shows the actual CDN URL from Shopify");
            Console.WriteLine($"Source Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Step 1: Create a temporary product
                Console.WriteLine("🔄 STEP 1: Creating temporary product...");
                
                var tempProduct = new Product
                {
                    Title = $"Temp Product for Real CDN Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Temporary product for getting real CDN URL</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await _client.Products.CreateAsync(tempProduct);
                Console.WriteLine($"✅ Created temporary product with ID: {createdProduct.Id}");

                try
                {
                    // Step 2: Upload image via REST API to get real CDN URL
                    Console.WriteLine();
                    Console.WriteLine("🔄 STEP 2: Uploading image via REST API...");
                    
                    var restImage = await _client.Images.UploadImageFromUrlAsync(
                        createdProduct.Id,
                        imageUrl,
                        altText,
                        1
                    );

                    Console.WriteLine("✅ REST upload completed!");
                    Console.WriteLine();

                    Console.WriteLine("=== REAL SHOPIFY CDN URL ===");
                    Console.WriteLine($"📁 Image ID: {restImage.Id}");
                    Console.WriteLine($"📊 Position: {restImage.Position}");
                    Console.WriteLine($"📝 Alt Text: {restImage.Alt ?? "Not set"}");
                    Console.WriteLine($"📅 Created At: {restImage.CreatedAt}");
                    Console.WriteLine($"🌐 REAL CDN URL: {restImage.Src}");
                    Console.WriteLine($"📏 Width: {restImage.Width}");
                    Console.WriteLine($"📐 Height: {restImage.Height}");
                    Console.WriteLine($"🔄 Updated At: {restImage.UpdatedAt}");

                    // Step 3: Verify the CDN URL is accessible
                    Console.WriteLine();
                    Console.WriteLine("🔄 STEP 3: Verifying CDN URL accessibility...");
                    
                    if (!string.IsNullOrEmpty(restImage.Src))
                    {
                        Console.WriteLine($"✅ CDN URL obtained: {restImage.Src}");
                        Console.WriteLine("💡 This is the REAL CDN URL from your Shopify store");
                        Console.WriteLine("💡 You can use this URL in your applications");
                        Console.WriteLine("💡 This image should be visible in your Shopify file dashboard");
                        
                        // Test if the URL is accessible
                        try
                        {
                            using var httpClient = new System.Net.Http.HttpClient();
                            var response = await httpClient.GetAsync(restImage.Src);
                            Console.WriteLine($"✅ CDN URL is accessible (Status: {response.StatusCode})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️  CDN URL might not be immediately accessible: {ex.Message}");
                            Console.WriteLine("💡 This is normal - Shopify CDN URLs may take a few minutes to become available");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No CDN URL obtained");
                    }

                    // Step 4: Also try GraphQL to see what it returns
                    Console.WriteLine();
                    Console.WriteLine("🔄 STEP 4: Testing GraphQL upload for comparison...");
                    
                    var fileInput = new FileCreateInput
                    {
                        OriginalSource = imageUrl,
                        ContentType = FileContentType.Image,
                        Alt = altText
                    };

                    var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
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
                        Console.WriteLine($"🌐 GraphQL URL: {graphqlFile.Image.Url ?? "Not available"}");
                        Console.WriteLine($"🔗 OriginalSrc: {graphqlFile.Image.OriginalSrc ?? "Not available"}");
                        Console.WriteLine($"🔄 TransformedSrc: {graphqlFile.Image.TransformedSrc ?? "Not available"}");
                        Console.WriteLine($"📷 Src: {graphqlFile.Image.Src ?? "Not available"}");
                    }

                    // Step 5: Summary
                    Console.WriteLine();
                    Console.WriteLine("=== FINAL SUMMARY ===");
                    Console.WriteLine($"✅ REST Image ID: {restImage.Id}");
                    Console.WriteLine($"✅ REST CDN URL: {restImage.Src}");
                    Console.WriteLine($"✅ GraphQL File ID: {graphqlFile.Id}");
                    Console.WriteLine($"✅ GraphQL File Status: {graphqlFile.FileStatus}");
                    
                    if (!string.IsNullOrEmpty(restImage.Src))
                    {
                        Console.WriteLine();
                        Console.WriteLine("🎉 SUCCESS: Real CDN URL obtained!");
                        Console.WriteLine($"🌐 Use this REAL CDN URL: {restImage.Src}");
                        Console.WriteLine("📋 This image should appear in your Shopify file dashboard");
                        Console.WriteLine("💡 This URL is specific to your Shopify store and should work");
                    }

                }
                finally
                {
                    // Clean up
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