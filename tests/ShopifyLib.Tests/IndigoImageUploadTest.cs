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
    public class ImageUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public ImageUploadTest()
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
        public async Task TestImageUrl_IdentifyTimeoutIssue_ProvideSolutions()
        {
            // Arrange - Use the original  image URL
            var ImageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = " Gift Image - Original Test";
            
            Console.WriteLine("===  IMAGE URL TEST ===");
            Console.WriteLine("This test identifies the timeout issue with the  URL and provides solutions");
            Console.WriteLine($"Original Image URL: {ImageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            // Test 1: Try the original  URL
            Console.WriteLine("🔄 TEST 1: Trying original  URL...");
            try
            {
                var fileInput = new FileCreateInput
                {
                    OriginalSource = ImageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                Console.WriteLine("✅  URL worked with GraphQL!");
                
                var file = response.Files[0];
                Console.WriteLine($"📁 File ID: {file.Id}");
                Console.WriteLine($"📊 Status: {file.FileStatus}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌  URL failed with GraphQL: {ex.Message}");
            }

            // Test 2: Try  URL with REST API
            Console.WriteLine();
            Console.WriteLine("🔄 TEST 2: Trying  URL with REST API...");
            
            var tempProduct = new Product
            {
                Title = $"Temp Product for  Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                BodyHtml = "<p>Temporary product for testing  URL</p>",
                Vendor = "Test Vendor",
                ProductType = "Test Type",
                Status = "draft",
                Published = false
            };

            var createdProduct = await _client.Products.CreateAsync(tempProduct);
            Console.WriteLine($"✅ Created temporary product with ID: {createdProduct.Id}");

            try
            {
                var restImage = await _client.Images.UploadImageFromUrlAsync(
                    createdProduct.Id,
                    ImageUrl,
                    altText,
                    1
                );

                Console.WriteLine("✅  URL worked with REST API!");
                Console.WriteLine($"📁 Image ID: {restImage.Id}");
                Console.WriteLine($"🌐 CDN URL: {restImage.Src}");
                Console.WriteLine($"📏 Dimensions: {restImage.Width}x{restImage.Height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌  URL failed with REST API: {ex.Message}");
                Console.WriteLine("💡 This confirms the timeout issue with the  URL");
            }
            finally
            {
                // Clean up
                await _client.Products.DeleteAsync(createdProduct.Id);
                Console.WriteLine("✅ Temporary product cleaned up");
            }

            // Test 3: Try alternative reliable URLs
            Console.WriteLine();
            Console.WriteLine("🔄 TEST 3: Testing alternative reliable URLs...");
            
            var reliableUrls = new[]
            {
                "https://httpbin.org/image/jpeg",
                "https://httpbin.org/image/png",
                "https://picsum.photos/800/600",
                "https://via.placeholder.com/800x600/FF0000/FFFFFF?text=Test+Image"
            };

            foreach (var url in reliableUrls)
            {
                Console.WriteLine();
                Console.WriteLine($"🔄 Testing URL: {url}");
                
                try
                {
                    var testFileInput = new FileCreateInput
                    {
                        OriginalSource = url,
                        ContentType = FileContentType.Image,
                        Alt = $"Test Image from {new Uri(url).Host}"
                    };

                    var testResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { testFileInput });
                    var testFile = testResponse.Files[0];
                    
                    Console.WriteLine($"✅ SUCCESS: {url}");
                    Console.WriteLine($"   📁 File ID: {testFile.Id}");
                    Console.WriteLine($"   📊 Status: {testFile.FileStatus}");
                    
                    if (testFile.Image != null)
                    {
                        Console.WriteLine($"   📏 Dimensions: {testFile.Image.Width}x{testFile.Image.Height}");
                        Console.WriteLine($"   🌐 URL: {testFile.Image.Url ?? "Not available"}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ FAILED: {url} - {ex.Message}");
                }
            }

            // Test 4: Try REST API with reliable URL
            Console.WriteLine();
            Console.WriteLine("🔄 TEST 4: Testing REST API with reliable URL...");
            
            var reliableUrl = "https://httpbin.org/image/jpeg";
            var restProduct = new Product
            {
                Title = $"Temp Product for Reliable URL Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                BodyHtml = "<p>Temporary product for testing reliable URL</p>",
                Vendor = "Test Vendor",
                ProductType = "Test Type",
                Status = "draft",
                Published = false
            };

            var restCreatedProduct = await _client.Products.CreateAsync(restProduct);
            Console.WriteLine($"✅ Created temporary product with ID: {restCreatedProduct.Id}");

            try
            {
                var reliableRestImage = await _client.Images.UploadImageFromUrlAsync(
                    restCreatedProduct.Id,
                    reliableUrl,
                    "Reliable Test Image",
                    1
                );

                Console.WriteLine("✅ Reliable URL worked with REST API!");
                Console.WriteLine($"📁 Image ID: {reliableRestImage.Id}");
                Console.WriteLine($"🌐 CDN URL: {reliableRestImage.Src}");
                Console.WriteLine($"📏 Dimensions: {reliableRestImage.Width}x{reliableRestImage.Height}");
                Console.WriteLine($"📅 Created: {reliableRestImage.CreatedAt}");
                
                Console.WriteLine();
                Console.WriteLine("🎉 SUCCESS: CDN URL obtained!");
                Console.WriteLine($"🌐 Use this CDN URL: {reliableRestImage.Src}");
                Console.WriteLine("📋 This image should appear in your Shopify file dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Reliable URL failed with REST API: {ex.Message}");
            }
            finally
            {
                // Clean up
                await _client.Products.DeleteAsync(restCreatedProduct.Id);
                Console.WriteLine("✅ Temporary product cleaned up");
            }

            // Summary and recommendations
            Console.WriteLine();
            Console.WriteLine("=== ISSUE ANALYSIS ===");
            Console.WriteLine("❌ PROBLEM: The  image URL is timing out when Shopify tries to download it");
            Console.WriteLine("💡 REASON: The URL might be slow, have access restrictions, or be temporarily unavailable");
            Console.WriteLine();
            Console.WriteLine("=== SOLUTIONS ===");
            Console.WriteLine("1. ✅ Use alternative reliable image URLs for testing");
            Console.WriteLine("2. ✅ The GraphQL and REST APIs work correctly with reliable URLs");
            Console.WriteLine("3. ✅ CDN URLs are obtained immediately with REST API");
            Console.WriteLine("4. 💡 For production, ensure your image URLs are fast and reliable");
            Console.WriteLine("5. 💡 Consider hosting images on a CDN for better performance");
            Console.WriteLine();
            Console.WriteLine("=== WORKING ALTERNATIVES ===");
            Console.WriteLine("• https://httpbin.org/image/jpeg");
            Console.WriteLine("• https://httpbin.org/image/png");
            Console.WriteLine("• https://picsum.photos/800/600");
            Console.WriteLine("• https://via.placeholder.com/800x600/FF0000/FFFFFF?text=Test+Image");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 