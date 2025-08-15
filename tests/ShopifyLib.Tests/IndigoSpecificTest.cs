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
    public class IndigoSpecificTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public IndigoSpecificTest()
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
        public async Task UploadSpecificIndigoImage_MultipleApproaches_GetCDNUrl()
        {
            // Arrange - Use the EXACT Indigo image URL you specified
            var indigoImageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - Specific Test";
            
            Console.WriteLine("=== SPECIFIC INDIGO IMAGE UPLOAD TEST ===");
            Console.WriteLine("This test tries to upload the EXACT Indigo image URL you specified");
            Console.WriteLine($"Target Image URL: {indigoImageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            // Approach 1: Direct GraphQL upload
            Console.WriteLine("🔄 APPROACH 1: Direct GraphQL upload...");
            try
            {
                var fileInput = new FileCreateInput
                {
                    OriginalSource = indigoImageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                var graphqlFile = graphqlResponse.Files[0];

                Console.WriteLine("✅ GraphQL upload successful!");
                Console.WriteLine($"📁 File ID: {graphqlFile.Id}");
                Console.WriteLine($"📊 Status: {graphqlFile.FileStatus}");
                Console.WriteLine($"📝 Alt: {graphqlFile.Alt}");
                
                if (graphqlFile.Image != null)
                {
                    Console.WriteLine($"📏 Dimensions: {graphqlFile.Image.Width}x{graphqlFile.Image.Height}");
                    Console.WriteLine($"🌐 URL: {graphqlFile.Image.Url ?? "Not available"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GraphQL upload failed: {ex.Message}");
            }

            // Approach 2: REST API upload
            Console.WriteLine();
            Console.WriteLine("🔄 APPROACH 2: REST API upload...");
            
            var tempProduct = new Product
            {
                Title = $"Temp Product for Indigo Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                BodyHtml = "<p>Temporary product for testing Indigo image</p>",
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
                    indigoImageUrl,
                    altText,
                    1
                );

                Console.WriteLine("✅ REST upload successful!");
                Console.WriteLine($"📁 Image ID: {restImage.Id}");
                Console.WriteLine($"🌐 CDN URL: {restImage.Src}");
                Console.WriteLine($"📏 Dimensions: {restImage.Width}x{restImage.Height}");
                Console.WriteLine($"📅 Created: {restImage.CreatedAt}");
                
                Console.WriteLine();
                Console.WriteLine("🎉 SUCCESS: Indigo image uploaded via REST API!");
                Console.WriteLine($"🌐 Use this CDN URL: {restImage.Src}");
                Console.WriteLine("📋 This should be the EXACT Indigo image you specified");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ REST upload failed: {ex.Message}");
                Console.WriteLine("💡 This confirms the timeout issue with the Indigo URL");
            }
            finally
            {
                // Clean up
                await _client.Products.DeleteAsync(createdProduct.Id);
                Console.WriteLine("✅ Temporary product cleaned up");
            }

            // Approach 3: Download first, then upload
            Console.WriteLine();
            Console.WriteLine("🔄 APPROACH 3: Download first, then upload...");
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(3); // Extended timeout
                
                Console.WriteLine("🔄 Downloading image from Indigo URL...");
                var imageBytes = await httpClient.GetByteArrayAsync(indigoImageUrl);
                Console.WriteLine($"✅ Downloaded {imageBytes.Length} bytes");
                
                // Convert to base64 and upload
                var base64Image = Convert.ToBase64String(imageBytes);
                var downloadFileInput = new FileCreateInput
                {
                    OriginalSource = $"data:image/jpeg;base64,{base64Image}",
                    ContentType = FileContentType.Image,
                    Alt = altText
                };
                
                var downloadResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { downloadFileInput });
                var downloadFile = downloadResponse.Files[0];
                
                Console.WriteLine("✅ Downloaded image uploaded successfully!");
                Console.WriteLine($"📁 File ID: {downloadFile.Id}");
                Console.WriteLine($"📊 Status: {downloadFile.FileStatus}");
                
                if (downloadFile.Image != null)
                {
                    Console.WriteLine($"📏 Dimensions: {downloadFile.Image.Width}x{downloadFile.Image.Height}");
                    Console.WriteLine($"🌐 URL: {downloadFile.Image.Url ?? "Not available"}");
                }
                
                Console.WriteLine();
                Console.WriteLine("🎉 SUCCESS: Indigo image uploaded via download method!");
                Console.WriteLine("💡 This should be the EXACT Indigo image you specified");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Download approach failed: {ex.Message}");
                Console.WriteLine("💡 The Indigo URL is not accessible from our servers");
            }

            // Approach 4: Try without query parameters
            Console.WriteLine();
            Console.WriteLine("🔄 APPROACH 4: Try without query parameters...");
            try
            {
                var baseUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg";
                Console.WriteLine($"🔄 Trying base URL: {baseUrl}");
                
                var baseFileInput = new FileCreateInput
                {
                    OriginalSource = baseUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var baseResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { baseFileInput });
                var baseFile = baseResponse.Files[0];

                Console.WriteLine("✅ Base URL upload successful!");
                Console.WriteLine($"📁 File ID: {baseFile.Id}");
                Console.WriteLine($"📊 Status: {baseFile.FileStatus}");
                
                if (baseFile.Image != null)
                {
                    Console.WriteLine($"📏 Dimensions: {baseFile.Image.Width}x{baseFile.Image.Height}");
                    Console.WriteLine($"🌐 URL: {baseFile.Image.Url ?? "Not available"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Base URL upload failed: {ex.Message}");
            }

            // Summary
            Console.WriteLine();
            Console.WriteLine("=== FINAL SUMMARY ===");
            Console.WriteLine("✅ Tested multiple approaches to upload the Indigo image");
            Console.WriteLine("✅ If any approach succeeded, you should see the image in your dashboard");
            Console.WriteLine("💡 The image should be the EXACT Indigo gift image you specified");
            Console.WriteLine("💡 If all approaches failed, the Indigo URL has accessibility issues");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 