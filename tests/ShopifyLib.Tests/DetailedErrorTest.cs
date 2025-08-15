using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib.Models;
using ShopifyLib.Services;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using ShopifyLib.Configuration;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Test class for capturing detailed error information from Shopify
    /// This helps identify exactly what Shopify is complaining about during image uploads
    /// </summary>
    [IntegrationTest]
    public class DetailedErrorTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public DetailedErrorTest()
        {
            // Load configuration from appsettings.json and environment variables (same as HybridImageUploadTest)
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
        public async Task CaptureDetailedErrors_IndigoImage_ShowAllValidationIssues()
        {
            // Arrange - Use the sample image URL
            //var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var imageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            var altText = "Sample Image - Error Test";
            
            Console.WriteLine("=== DETAILED ERROR CAPTURE TEST ===");
            Console.WriteLine("This test captures ALL error details from Shopify to identify validation issues");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Test 1: GraphQL Upload with Detailed Error Capture
                Console.WriteLine("🔄 TEST 1: GraphQL Upload with Error Capture...");
                await TestGraphQLUploadWithErrors(imageUrl, altText);

                Console.WriteLine();
                Console.WriteLine("🔄 TEST 2: REST API Upload with Error Capture...");
                await TestRESTUploadWithErrors(imageUrl, altText);

                Console.WriteLine();
                Console.WriteLine("🔄 TEST 3: Alternative URLs for Comparison...");
                await TestAlternativeUrls();

                Console.WriteLine();
                Console.WriteLine("🔄 TEST 4: Image Validation Check...");
                await TestImageValidation(imageUrl);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
                throw;
            }
        }

        private async Task TestGraphQLUploadWithErrors(string imageUrl, string altText)
        {
            try
            {
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                
                if (response.Files != null && response.Files.Count > 0 && response.Files[0] != null)
                {
                    Console.WriteLine("✅ GraphQL upload succeeded!");
                    Console.WriteLine($"📁 File ID: {response.Files[0].Id}");
                    Console.WriteLine($"📊 Status: {response.Files[0].FileStatus}");
                    if (response.Files[0].Image != null)
                    {
                        Console.WriteLine($"📏 Dimensions: {response.Files[0].Image.Width}x{response.Files[0].Image.Height}");
                        Console.WriteLine($"🌐 URL: {response.Files[0].Image.Url ?? "Not available"}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No file returned in GraphQL response.");
                }
            }
            catch (GraphQLUserException ex)
            {
                Console.WriteLine("❌ GraphQL User Errors:");
                Console.WriteLine($"Error Message: {ex.Message}");
                if (ex.UserErrors != null)
                {
                    foreach (var error in ex.UserErrors)
                    {
                        Console.WriteLine($"  • Field: {string.Join(",", error.Field)}");
                        Console.WriteLine($"  • Message: {error.Message}");
                    }
                }
            }
            catch (GraphQLException ex)
            {
                Console.WriteLine("❌ GraphQL Errors:");
                Console.WriteLine($"Error Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected GraphQL Error: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private async Task TestRESTUploadWithErrors(string imageUrl, string altText)
        {
            try
            {
                // Create a temporary product
                var tempProduct = new Product
                {
                    Title = $"Error Test Product {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Temporary product for error testing</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await _client.Products.CreateAsync(tempProduct);
                Console.WriteLine($"✅ Created temporary product: {createdProduct.Id}");

                try
                {
                    var image = await _client.Images.UploadImageFromUrlAsync(
                        createdProduct.Id,
                        imageUrl,
                        altText,
                        1
                    );

                    Console.WriteLine("✅ REST upload succeeded!");
                    Console.WriteLine($"📁 Image ID: {image.Id}");
                    Console.WriteLine($"🌐 SRC URL: {image.Src ?? "Not available"}");
                    Console.WriteLine($"📏 Dimensions: {image.Width}x{image.Height}");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("❌ REST API Error:");
                    Console.WriteLine($"Error Message: {ex.Message}");
                    Console.WriteLine($"Type: {ex.GetType().Name}");
                    
                    // Try to extract more details from the error message
                    if (ex.Message.Contains("{"))
                    {
                        try
                        {
                            var startIndex = ex.Message.IndexOf('{');
                            var endIndex = ex.Message.LastIndexOf('}') + 1;
                            var jsonError = ex.Message.Substring(startIndex, endIndex - startIndex);
                            
                            Console.WriteLine("📋 Parsed Error JSON:");
                            var parsedError = JsonConvert.DeserializeObject<dynamic>(jsonError);
                            Console.WriteLine(JsonConvert.SerializeObject(parsedError, Formatting.Indented));
                        }
                        catch
                        {
                            Console.WriteLine("📋 Raw Error Content (could not parse JSON):");
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Unexpected REST Error: {ex.Message}");
                    Console.WriteLine($"Type: {ex.GetType().Name}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }
                finally
                {
                    // Clean up
                    await _client.Products.DeleteAsync(createdProduct.Id);
                    Console.WriteLine("✅ Temporary product cleaned up");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create test product: {ex.Message}");
            }
        }

        private async Task TestAlternativeUrls()
        {
            var testUrls = new[]
            {
                "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg",
                "https://via.placeholder.com/800x600/FF0000/FFFFFF?text=Test+Image",
                "https://picsum.photos/800/600"
            };

            foreach (var url in testUrls)
            {
                Console.WriteLine($"🔄 Testing URL: {url}");
                
                try
                {
                    var fileInput = new FileCreateInput
                    {
                        OriginalSource = url,
                        ContentType = FileContentType.Image,
                        Alt = $"Test Image - {url}"
                    };

                    var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                    Console.WriteLine($"✅ SUCCESS: {response.Files[0].Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ FAILED: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        private async Task TestImageValidation(string imageUrl)
        {
            Console.WriteLine("🔄 Testing image validation and accessibility...");
            
            try
            {
                // Test if we can access the image directly
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                Console.WriteLine("🔄 Checking image accessibility...");
                var response = await httpClient.GetAsync(imageUrl);
                
                Console.WriteLine($"📊 HTTP Status: {response.StatusCode}");
                Console.WriteLine($"📊 Content Type: {response.Content.Headers.ContentType}");
                Console.WriteLine($"📊 Content Length: {response.Content.Headers.ContentLength}");
                
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    Console.WriteLine($"📊 Actual Size: {imageBytes.Length} bytes");
                    
                    // Check if it's a valid image
                    if (imageBytes.Length > 0)
                    {
                        var contentType = response.Content.Headers.ContentType?.ToString();
                        Console.WriteLine($"✅ Image is accessible and has content type: {contentType}");
                        
                        // Try to upload the downloaded image
                        Console.WriteLine("🔄 Attempting to upload downloaded image...");
                        var base64Image = Convert.ToBase64String(imageBytes);
                        var fileInput = new FileCreateInput
                        {
                            OriginalSource = $"data:image/jpeg;base64,{base64Image}",
                            ContentType = FileContentType.Image,
                            Alt = "Downloaded Indigo Image"
                        };
                        
                        var uploadResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                        Console.WriteLine($"✅ Downloaded image upload successful: {uploadResponse.Files[0].Id}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Image is not accessible: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"📋 Error Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Image validation failed: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 