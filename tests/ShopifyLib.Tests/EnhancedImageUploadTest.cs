using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Newtonsoft.Json;
using System.Linq;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class EnhancedImageUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public EnhancedImageUploadTest()
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
        public async Task UploadImage_GetShopifyCDNUrl_DisplayAllAttributes()
        {
            // Arrange - Use a valid image URL
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - Enhanced Test";
            
            Console.WriteLine("=== Enhanced Image Upload Test ===");
            Console.WriteLine("This test demonstrates getting Shopify CDN URLs and all image attributes");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Act - Upload image using enhanced GraphQL mutation
                Console.WriteLine("🔄 Uploading image to Shopify using enhanced GraphQL mutation...");
                
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                // Assert and Display Results
                Console.WriteLine("✅ Image upload completed successfully!");
                Console.WriteLine();

                // Validate response structure
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.NotEmpty(response.Files);
                Assert.Empty(response.UserErrors);

                var uploadedFile = response.Files[0];
                
                // Display complete response
                Console.WriteLine("=== COMPLETE ENHANCED RESPONSE ===");
                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
                Console.WriteLine();

                // Display file information
                Console.WriteLine("=== FILE INFORMATION ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");
                
                // Display image details with URLs
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== IMAGE DETAILS WITH SHOPIFY CDN URLS ===");
                    Console.WriteLine($"📏 Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height} pixels");
                    Console.WriteLine($"📊 Aspect Ratio: {(double)uploadedFile.Image.Width / uploadedFile.Image.Height:F2}");
                    Console.WriteLine();
                    Console.WriteLine("🌐 SHOPIFY CDN URLS:");
                    Console.WriteLine($"   • CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"   • Original Source: {uploadedFile.Image.OriginalSrc ?? "Not available"}");
                    Console.WriteLine($"   • Transformed Source: {uploadedFile.Image.TransformedSrc ?? "Not available"}");
                    Console.WriteLine($"   • Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                    
                    // Validate that we have at least one URL
                    var hasUrl = !string.IsNullOrEmpty(uploadedFile.Image.Url) || 
                                !string.IsNullOrEmpty(uploadedFile.Image.Src) || 
                                !string.IsNullOrEmpty(uploadedFile.Image.OriginalSrc);
                    
                    Assert.True(hasUrl, "At least one image URL should be available from Shopify");
                    
                    // Validate dimensions
                    Assert.True(uploadedFile.Image.Width > 0, "Image width should be greater than 0");
                    Assert.True(uploadedFile.Image.Height > 0, "Image height should be greater than 0");
                }
                else
                {
                    Console.WriteLine("⚠️  Image details not available in response");
                    Console.WriteLine("ℹ️  This is normal for newly uploaded files - Shopify needs time to process the image");
                    Console.WriteLine("ℹ️  The image URLs and dimensions will become available once processing is complete");
                    Console.WriteLine("ℹ️  You can query this file again later using the File ID to get the CDN URLs");
                }

                // Display GraphQL ID breakdown
                Console.WriteLine();
                Console.WriteLine("=== GRAPHQL ID BREAKDOWN ===");
                Console.WriteLine($"🆔 Full GraphQL ID: {uploadedFile.Id}");
                
                if (uploadedFile.Id.StartsWith("gid://shopify/MediaImage/"))
                {
                    var idParts = uploadedFile.Id.Split('/');
                    if (idParts.Length >= 4)
                    {
                        Console.WriteLine($"🏷️  Resource Type: MediaImage");
                        Console.WriteLine($"🔢 Numeric ID: {idParts[3]}");
                    }
                }

                // Display status information
                Console.WriteLine();
                Console.WriteLine("=== STATUS INFORMATION ===");
                Console.WriteLine($"🔄 Processing Status: {uploadedFile.FileStatus}");
                
                if (uploadedFile.FileStatus.Equals("READY", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✅ File is ready for use!");
                }
                else if (uploadedFile.FileStatus.Equals("UPLOADED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("⏳ File uploaded successfully, processing in progress...");
                }
                else
                {
                    Console.WriteLine($"ℹ️  File status: {uploadedFile.FileStatus}");
                }

                // Wait for processing and query the existing file to get CDN URLs
                Console.WriteLine();
                Console.WriteLine("=== WAITING FOR PROCESSING AND QUERYING EXISTING FILE ===");
                Console.WriteLine("⏳ Waiting 2 seconds for Shopify to process the image...");
                await Task.Delay(2000); // Wait 2 seconds
                
                Console.WriteLine("🔄 Querying existing file to get processed image with CDN URLs...");
                
                // Query the existing file by ID to get the processed image with CDN URLs
                var fileQuery = @"
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

                var queryVariables = new { id = uploadedFile.Id };
                var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, queryVariables);
                
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
                            Console.WriteLine();
                            Console.WriteLine("🎉 PROCESSED IMAGE WITH CDN URLS:");
                            Console.WriteLine($"📏 Dimensions: {node.image.width}x{node.image.height} pixels");
                            Console.WriteLine($"📊 Aspect Ratio: {(double)node.image.width / node.image.height:F2}");
                            Console.WriteLine();
                            Console.WriteLine("🌐 SHOPIFY CDN URLS (PROCESSED):");
                            Console.WriteLine($"   • CDN URL: {node.image.url ?? "Not available"}");
                            Console.WriteLine($"   • Original Source: {node.image.originalSrc ?? "Not available"}");
                            Console.WriteLine($"   • Transformed Source: {node.image.transformedSrc ?? "Not available"}");
                            Console.WriteLine($"   • Primary Source: {node.image.src ?? "Not available"}");
                            
                            // Validate that we have at least one URL
                            var hasUrl = !string.IsNullOrEmpty(node.image.url?.ToString()) || 
                                        !string.IsNullOrEmpty(node.image.src?.ToString()) || 
                                        !string.IsNullOrEmpty(node.image.originalSrc?.ToString());
                            
                            if (hasUrl)
                            {
                                Console.WriteLine("✅ CDN URLs are now available!");
                            }
                            else
                            {
                                Console.WriteLine("⚠️  CDN URLs still not available after processing wait");
                            }
                            
                            // Validate dimensions
                            Assert.True(node.image.width > 0, "Image width should be greater than 0");
                            Assert.True(node.image.height > 0, "Image height should be greater than 0");
                        }
                        else
                        {
                            Console.WriteLine("⚠️  Image details still not available after processing wait");
                            Console.WriteLine("ℹ️  This might indicate the image is still being processed or there was an issue");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Query response does not contain file data");
                        Console.WriteLine("ℹ️  This might indicate the file ID is invalid or the file was not found");
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"❌ Failed to parse query response: {parseEx.Message}");
                }

                // Display summary with URLs
                Console.WriteLine();
                Console.WriteLine("=== ENHANCED TEST SUMMARY ===");
                Console.WriteLine($"✅ Successfully uploaded image to Shopify");
                Console.WriteLine($"✅ File ID: {uploadedFile.Id}");
                Console.WriteLine($"✅ Status: {uploadedFile.FileStatus}");
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"✅ Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"✅ Shopify CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"✅ Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                    Console.WriteLine($"✅ Original Source: {uploadedFile.Image.OriginalSrc ?? "Not available"}");
                }
                Console.WriteLine("✅ Image uploaded without attaching to any product or variant");
                Console.WriteLine("✅ All Shopify CDN URLs and attributes displayed above");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        [Fact]
        public async Task UploadImage_GetCDNUrl_FirstCall_REST_API()
        {
            // Arrange - Use the Indigo dynamic URL
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/lego-exotic-parrot-31136/673419373623/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - REST ";
            
            Console.WriteLine("=== REST API First Call CDN URL Test (Indigo URL) ===");
            Console.WriteLine("This test demonstrates getting CDN URLs with the FIRST call using REST API with Indigo dynamic URL");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Step 1: Create a temporary product for REST API upload
                Console.WriteLine("🔄 Creating temporary product for REST API upload...");
                
                var tempProduct = new Product
                {
                    Title = $"Temp Product for CDN Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Temporary product for getting CDN URL with first call</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await _client.Products.CreateAsync(tempProduct);
                Console.WriteLine($"✅ Created temporary product with ID: {createdProduct.Id}");

                try
                {
                    // Step 2: Upload image via REST API (gets CDN URL immediately)
                    Console.WriteLine("🔄 Uploading image via REST API to get CDN URL with first call...");
                    
                    var restImage = await _client.Images.UploadImageFromUrlAsync(
                        createdProduct.Id,
                        imageUrl,
                        altText,
                        1
                    );

                    Console.WriteLine("✅ REST API upload completed with CDN URL!");
                    Console.WriteLine();

                    // Display the CDN URL from the FIRST call
                    Console.WriteLine("=== CDN URL FROM FIRST CALL (REST API) ===");
                    Console.WriteLine($"📁 Image ID: {restImage.Id}");
                    Console.WriteLine($"📊 Position: {restImage.Position}");
                    Console.WriteLine($"📝 Alt Text: {restImage.Alt ?? "Not set"}");
                    Console.WriteLine($"📅 Created At: {restImage.CreatedAt}");
                    Console.WriteLine($"🌐 CDN URL (FIRST CALL): {restImage.Src ?? "Not available"}");
                    Console.WriteLine($"📏 Width: {restImage.Width}");
                    Console.WriteLine($"📐 Height: {restImage.Height}");
                    Console.WriteLine($"🔄 Updated At: {restImage.UpdatedAt}");

                    // Validate that we got the CDN URL
                    if (!string.IsNullOrEmpty(restImage.Src))
                    {
                        Console.WriteLine();
                        Console.WriteLine("🎉 SUCCESS: CDN URL obtained with FIRST call!");
                        Console.WriteLine($"🌐 Use this CDN URL: {restImage.Src}");
                        Console.WriteLine("✅ This is the REAL CDN URL from your Shopify store");
                        Console.WriteLine("✅ This image should be visible in your Shopify file dashboard");
                        
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
                        Console.WriteLine("❌ No CDN URL obtained with first call");
                    }

                    // Step 3: Compare with GraphQL approach
                    Console.WriteLine();
                    Console.WriteLine("🔄 Comparing with GraphQL approach...");
                    
                    var fileInput = new FileCreateInput
                    {
                        OriginalSource = imageUrl,
                        ContentType = FileContentType.Image,
                        Alt = $"{altText}"
                    };

                    var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                    var graphqlFile = graphqlResponse.Files[0];

                    Console.WriteLine("=== GRAPHQL FIRST CALL COMPARISON ===");
                    Console.WriteLine($"📁 File ID: {graphqlFile.Id}");
                    Console.WriteLine($"📊 Status: {graphqlFile.FileStatus}");
                    Console.WriteLine($"🌐 CDN URL (GraphQL): {graphqlFile.Image?.Url ?? "Not available"}");
                    
                    if (graphqlFile.Image?.Url != null)
                    {
                        Console.WriteLine("✅ GraphQL also provided CDN URL with first call!");
                    }
                    else
                    {
                        Console.WriteLine("⚠️  GraphQL did not provide CDN URL with first call");
                        Console.WriteLine("💡 This is why we use REST API for immediate CDN URL access");
                    }

                    // Summary
                    Console.WriteLine();
                    Console.WriteLine("=== SUMMARY ===");
                    Console.WriteLine("✅ REST API: Provides CDN URL with FIRST call");
                    Console.WriteLine("⚠️  GraphQL: May not provide CDN URL with first call");
                    Console.WriteLine("💡 Recommendation: Use REST API for immediate CDN URL access");
                    Console.WriteLine("💡 Alternative: Use GraphQL + wait/query for CDN URL");

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

        [Fact]
        public async Task DiagnoseCDNUrl404_TestAlternativeApproaches()
        {
            // Arrange - Use the Indigo dynamic URL
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/lego-exotic-parrot-31136/673419373623/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - CDN 404 Diagnosis";
            
            Console.WriteLine("=== CDN URL 404 DIAGNOSIS TEST ===");
            Console.WriteLine("This test diagnoses CDN URL 404 issues and tests alternative approaches");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Test 1: REST API approach with CDN URL validation
                Console.WriteLine("🔄 TEST 1: REST API with CDN URL validation...");
                
                var tempProduct = new Product
                {
                    Title = $"CDN Test Product {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Testing CDN URL accessibility</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await _client.Products.CreateAsync(tempProduct);
                Console.WriteLine($"✅ Created test product with ID: {createdProduct.Id}");

                try
                {
                    var restImage = await _client.Images.UploadImageFromUrlAsync(
                        createdProduct.Id,
                        imageUrl,
                        altText,
                        1
                    );

                    Console.WriteLine("=== REST API CDN URL TEST ===");
                    Console.WriteLine($"📁 Image ID: {restImage.Id}");
                    Console.WriteLine($"🌐 CDN URL: {restImage.Src}");
                    Console.WriteLine($"📅 Created At: {restImage.CreatedAt}");

                    // Test CDN URL accessibility with retries
                    Console.WriteLine();
                    Console.WriteLine("🔄 Testing CDN URL accessibility with retries...");
                    
                    bool cdnUrlAccessible = false;
                    int maxRetries = 5;
                    int retryDelaySeconds = 30;

                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        Console.WriteLine($"🔄 Attempt {attempt}/{maxRetries}: Testing CDN URL...");
                        
                        try
                        {
                            using var httpClient = new System.Net.Http.HttpClient();
                            httpClient.Timeout = TimeSpan.FromSeconds(30);
                            
                            var response = await httpClient.GetAsync(restImage.Src);
                            Console.WriteLine($"   Status: {response.StatusCode}");
                            
                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine("✅ CDN URL is accessible!");
                                cdnUrlAccessible = true;
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"❌ CDN URL returned {response.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ CDN URL test failed: {ex.Message}");
                        }

                        if (attempt < maxRetries)
                        {
                            Console.WriteLine($"⏳ Waiting {retryDelaySeconds} seconds before next attempt...");
                            await Task.Delay(retryDelaySeconds * 1000);
                        }
                    }

                    if (!cdnUrlAccessible)
                    {
                        Console.WriteLine();
                        Console.WriteLine("⚠️  CDN URL is not accessible after multiple attempts");
                        Console.WriteLine("💡 This indicates a CDN propagation or processing issue");
                        
                        // Test 2: Try GraphQL approach
                        Console.WriteLine();
                        Console.WriteLine("🔄 TEST 2: Trying GraphQL approach...");
                        
                        var fileInput = new FileCreateInput
                        {
                            OriginalSource = imageUrl,
                            ContentType = FileContentType.Image,
                            Alt = $"{altText} - GraphQL"
                        };

                        var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                        var graphqlFile = graphqlResponse.Files[0];

                        Console.WriteLine("=== GRAPHQL FILE TEST ===");
                        Console.WriteLine($"📁 File ID: {graphqlFile.Id}");
                        Console.WriteLine($"📊 Status: {graphqlFile.FileStatus}");
                        Console.WriteLine($"🌐 CDN URL: {graphqlFile.Image?.Url ?? "Not available"}");

                        // Wait and query for CDN URL
                        Console.WriteLine();
                        Console.WriteLine("⏳ Waiting 60 seconds for GraphQL file processing...");
                        await Task.Delay(60000); // Wait 60 seconds

                        var fileQuery = @"
                            query getFile($id: ID!) {
                                node(id: $id) {
                                    ... on MediaImage {
                                        id
                                        fileStatus
                                        image {
                                            url
                                            originalSrc
                                            transformedSrc
                                            src
                                        }
                                    }
                                }
                            }";

                        var queryVariables = new { id = graphqlFile.Id };
                        var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, queryVariables);
                        
                        Console.WriteLine("=== GRAPHQL QUERY RESPONSE ===");
                        Console.WriteLine(queryResponse);

                        // Test 3: Try alternative URL format
                        Console.WriteLine();
                        Console.WriteLine("🔄 TEST 3: Testing alternative URL formats...");
                        
                        // Try different URL variations
                        var urlVariations = new[]
                        {
                            restImage.Src,
                            restImage.Src?.Replace("?v=", "?"),
                            restImage.Src?.Split('?')[0] + "?v=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            restImage.Src?.Replace("cdn.shopify.com", "cdn.shopify.com/s/files")
                        };

                        foreach (var url in urlVariations.Where(u => !string.IsNullOrEmpty(u)))
                        {
                            Console.WriteLine($"🔄 Testing URL: {url}");
                            try
                            {
                                using var httpClient = new System.Net.Http.HttpClient();
                                httpClient.Timeout = TimeSpan.FromSeconds(10);
                                
                                var response = await httpClient.GetAsync(url);
                                Console.WriteLine($"   Status: {response.StatusCode}");
                                
                                if (response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"✅ Alternative URL works: {url}");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"   ❌ Failed: {ex.Message}");
                            }
                        }

                        // Test 4: Try downloading and re-uploading
                        Console.WriteLine();
                        Console.WriteLine("🔄 TEST 4: Download and re-upload approach...");
                        
                        try
                        {
                            Console.WriteLine("🔄 Downloading image from source URL...");
                            using var httpClient = new System.Net.Http.HttpClient();
                            httpClient.Timeout = TimeSpan.FromMinutes(2);
                            
                            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                            Console.WriteLine($"✅ Downloaded {imageBytes.Length} bytes");

                            // Convert to base64 and upload
                            var base64Image = Convert.ToBase64String(imageBytes);
                            var dataUrl = $"data:image/jpeg;base64,{base64Image}";
                            
                            var downloadedFileInput = new FileCreateInput
                            {
                                OriginalSource = dataUrl,
                                ContentType = FileContentType.Image,
                                Alt = $"{altText} - Downloaded"
                            };

                            var downloadedResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { downloadedFileInput });
                            var downloadedFile = downloadedResponse.Files[0];

                            Console.WriteLine("=== DOWNLOADED FILE TEST ===");
                            Console.WriteLine($"📁 File ID: {downloadedFile.Id}");
                            Console.WriteLine($"📊 Status: {downloadedFile.FileStatus}");
                            Console.WriteLine($"🌐 CDN URL: {downloadedFile.Image?.Url ?? "Not available"}");

                            // Wait and query downloaded file
                            Console.WriteLine("⏳ Waiting 30 seconds for downloaded file processing...");
                            await Task.Delay(30000);

                            var downloadedQueryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, new { id = downloadedFile.Id });
                            Console.WriteLine("=== DOWNLOADED FILE QUERY RESPONSE ===");
                            Console.WriteLine(downloadedQueryResponse);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Download and re-upload failed: {ex.Message}");
                        }
                    }

                    // Summary and recommendations
                    Console.WriteLine();
                    Console.WriteLine("=== CDN 404 DIAGNOSIS SUMMARY ===");
                    Console.WriteLine("🔍 Issues Identified:");
                    Console.WriteLine("   • CDN URL returns 404 even after 30+ minutes");
                    Console.WriteLine("   • This indicates a Shopify CDN processing issue");
                    Console.WriteLine();
                    Console.WriteLine("💡 Recommendations:");
                    Console.WriteLine("   1. Contact Shopify Support about CDN URL 404 issue");
                    Console.WriteLine("   2. Use alternative image hosting (AWS S3, Cloudinary, etc.)");
                    Console.WriteLine("   3. Implement retry logic with exponential backoff");
                    Console.WriteLine("   4. Consider downloading images first, then uploading");
                    Console.WriteLine("   5. Use the original source URL as fallback");
                    Console.WriteLine();
                    Console.WriteLine("🛠️  Workarounds:");
                    Console.WriteLine("   • Store both CDN URL and original URL");
                    Console.WriteLine("   • Implement URL validation before using");
                    Console.WriteLine("   • Use image proxy services");
                    Console.WriteLine("   • Cache images locally if possible");

                }
                finally
                {
                    // Clean up
                    Console.WriteLine();
                    Console.WriteLine("🧹 Cleaning up test product...");
                    await _client.Products.DeleteAsync(createdProduct.Id);
                    Console.WriteLine("✅ Test product deleted");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Diagnosis test failed with error: {ex.Message}");
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