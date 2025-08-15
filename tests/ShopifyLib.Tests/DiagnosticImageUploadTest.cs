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
    public class DiagnosticImageUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public DiagnosticImageUploadTest()
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
        public async Task Diagnostic_CheckGraphQLResponse_IdentifyMissingURLs()
        {
            // Arrange - Use the specified Indigo image URL
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - Diagnostic Test";
            
            Console.WriteLine("=== DIAGNOSTIC IMAGE UPLOAD TEST ===");
            Console.WriteLine("This test will help identify why URLs aren't appearing");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Act - Upload image using GraphQL
                Console.WriteLine("🔄 Uploading image to Shopify using GraphQL...");
                
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                Console.WriteLine("✅ Image upload completed successfully!");
                Console.WriteLine();

                // Display the RAW response first
                Console.WriteLine("=== RAW GRAPHQL RESPONSE ===");
                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
                Console.WriteLine();

                // Validate response structure
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.NotEmpty(response.Files);
                Assert.Empty(response.UserErrors);

                var uploadedFile = response.Files[0];
                
                Console.WriteLine("=== DETAILED ANALYSIS ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");
                
                // Check if image object exists
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== IMAGE OBJECT ANALYSIS ===");
                    Console.WriteLine($"📏 Width: {uploadedFile.Image.Width}");
                    Console.WriteLine($"📐 Height: {uploadedFile.Image.Height}");
                    Console.WriteLine($"🌐 URL: {uploadedFile.Image.Url ?? "NULL"}");
                    Console.WriteLine($"🔗 OriginalSrc: {uploadedFile.Image.OriginalSrc ?? "NULL"}");
                    Console.WriteLine($"🔄 TransformedSrc: {uploadedFile.Image.TransformedSrc ?? "NULL"}");
                    Console.WriteLine($"📷 Src: {uploadedFile.Image.Src ?? "NULL"}");
                    
                    // Check if any URL is available
                    var hasAnyUrl = !string.IsNullOrEmpty(uploadedFile.Image.Url) ||
                                   !string.IsNullOrEmpty(uploadedFile.Image.OriginalSrc) ||
                                   !string.IsNullOrEmpty(uploadedFile.Image.TransformedSrc) ||
                                   !string.IsNullOrEmpty(uploadedFile.Image.Src);
                    
                    Console.WriteLine($"🔍 Has any URL: {hasAnyUrl}");
                    
                    if (!hasAnyUrl)
                    {
                        Console.WriteLine("⚠️  WARNING: No URLs found in the response!");
                        Console.WriteLine("   This suggests the GraphQL mutation might not be requesting the right fields.");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️  WARNING: Image object is null!");
                    Console.WriteLine("   This suggests the file might not be recognized as an image.");
                }

                // Check file status
                Console.WriteLine();
                Console.WriteLine("=== FILE STATUS ANALYSIS ===");
                Console.WriteLine($"🔄 File Status: {uploadedFile.FileStatus}");
                
                if (uploadedFile.FileStatus.Equals("READY", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✅ File is ready for use");
                }
                else if (uploadedFile.FileStatus.Equals("UPLOADED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("⏳ File uploaded, but still processing");
                    Console.WriteLine("   URLs might appear after processing is complete");
                }
                else
                {
                    Console.WriteLine($"ℹ️  File status: {uploadedFile.FileStatus}");
                }

                // Try to construct a potential CDN URL
                Console.WriteLine();
                Console.WriteLine("=== POTENTIAL CDN URL CONSTRUCTION ===");
                if (uploadedFile.Id.StartsWith("gid://shopify/MediaImage/"))
                {
                    var idParts = uploadedFile.Id.Split('/');
                    if (idParts.Length >= 4)
                    {
                        var numericId = idParts[3];
                        Console.WriteLine($"🔢 Numeric ID: {numericId}");
                        Console.WriteLine($"🏗️  Potential CDN URL pattern: https://cdn.shopify.com/s/files/1/[shop_id]/files/[filename]");
                        Console.WriteLine($"💡 Note: The actual CDN URL might need to be constructed differently");
                    }
                }

                // Summary
                Console.WriteLine();
                Console.WriteLine("=== DIAGNOSTIC SUMMARY ===");
                Console.WriteLine($"✅ File uploaded successfully");
                Console.WriteLine($"✅ File ID: {uploadedFile.Id}");
                Console.WriteLine($"✅ Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"❓ URLs available: {(uploadedFile.Image?.Url != null || uploadedFile.Image?.Src != null ? "Yes" : "No")}");
                Console.WriteLine($"❓ Image object exists: {uploadedFile.Image != null}");
                
                if (uploadedFile.Image == null || (string.IsNullOrEmpty(uploadedFile.Image.Url) && string.IsNullOrEmpty(uploadedFile.Image.Src)))
                {
                    Console.WriteLine();
                    Console.WriteLine("🔧 RECOMMENDATIONS:");
                    Console.WriteLine("1. Check if the GraphQL mutation is requesting the correct fields");
                    Console.WriteLine("2. Verify the file is being processed as an image");
                    Console.WriteLine("3. Wait for processing to complete if status is 'UPLOADED'");
                    Console.WriteLine("4. Check Shopify admin dashboard for the uploaded file");
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