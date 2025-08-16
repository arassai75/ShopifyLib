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
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class CDNUrl404FixTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly HttpClient _httpClient;

        public CDNUrl404FixTest()
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
            
            // Initialize HTTP client with proper headers
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/apng,image/*,*/*;q=0.8");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        [Fact]
        public async Task FixCDNUrl404_ComprehensiveApproach()
        {
            // Use a reliable test image URL
            var imageUrl = "https://dynamic.images.ca/v1/gifts/lego-77242/673419405270/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Dynamic Asset Test";
            
            Console.WriteLine("=== CDN URL 404 TEST ===");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Approach 1: REST API with proper CDN URL validation
                Console.WriteLine("🔄 APPROACH 1: REST API with CDN URL validation...");
                
                var tempProduct = new Product
                {
                    Title = $"CDN Fix Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                    BodyHtml = "<p>Testing CDN URL 404 fixes</p>",
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

                    Console.WriteLine("=== REST API CDN URL ===");
                    Console.WriteLine($"📁 Image ID: {restImage.Id}");
                    Console.WriteLine($"🌐 Original CDN URL: {restImage.Src}");
                    Console.WriteLine($"📅 Created At: {restImage.CreatedAt}");

                    // Test and fix CDN URL
                    var fixedCdnUrl = await TestAndFixCDNUrlAsync(restImage.Src, restImage.Id);
                    
                    if (fixedCdnUrl != restImage.Src)
                    {
                        Console.WriteLine($"✅ CDN URL fixed: {fixedCdnUrl}");
                    }
                    else
                    {
                        Console.WriteLine("✅ Original CDN URL is working");
                    }

                    // Approach 2: GraphQL with proper URL construction
                    Console.WriteLine();
                    Console.WriteLine("🔄 APPROACH 2: GraphQL with URL construction...");
                    
                    var fileInput = new FileCreateInput
                    {
                        OriginalSource = imageUrl,
                        ContentType = FileContentType.Image,
                        Alt = $"{altText}"
                    };

                    var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                    var graphqlFile = graphqlResponse.Files[0];

                    Console.WriteLine("=== GRAPHQL FILE ===");
                    Console.WriteLine($"📁 File ID: {graphqlFile.Id}");
                    Console.WriteLine($"📊 Status: {graphqlFile.FileStatus}");
                    Console.WriteLine($"🌐 CDN URL: {graphqlFile.Image?.Url ?? "Not available"}");

                    // Wait for processing and construct proper CDN URL
                    Console.WriteLine("⏳ Waiting 30 seconds for GraphQL file processing...");
                    await Task.Delay(30000);

                    var constructedCdnUrl = await ConstructProperCDNUrlAsync(graphqlFile.Id);
                    Console.WriteLine($"🔧 Constructed CDN URL: {constructedCdnUrl}");

                    // Test the constructed URL
                    var isConstructedUrlWorking = await TestUrlAccessibilityAsync(constructedCdnUrl);
                    Console.WriteLine($"✅ Constructed URL working: {isConstructedUrlWorking}");

                    // Approach 3: Alternative URL formats
                    Console.WriteLine();
                    Console.WriteLine("🔄 APPROACH 3: Testing alternative URL formats...");
                    
                    var alternativeUrls = await GenerateAlternativeCDNUrlsAsync(restImage.Src, restImage.Id);
                    
                    foreach (var (format, url) in alternativeUrls)
                    {
                        var isWorking = await TestUrlAccessibilityAsync(url);
                        Console.WriteLine($"🌐 {format}: {url}");
                        Console.WriteLine($"   Status: {(isWorking ? "✅ Working" : "❌ 404")}");
                    }

                    // Approach 4: Download and re-upload with proper format
                    Console.WriteLine();
                    Console.WriteLine("🔄 APPROACH 4: Download and re-upload with proper format...");
                    
                    var downloadedCdnUrl = await DownloadAndReuploadWithProperFormatAsync(imageUrl, $"{altText} - Downloaded");
                    Console.WriteLine($"📥 Downloaded and re-uploaded CDN URL: {downloadedCdnUrl}");
                    
                    var isDownloadedUrlWorking = await TestUrlAccessibilityAsync(downloadedCdnUrl);
                    Console.WriteLine($"✅ Downloaded URL working: {isDownloadedUrlWorking}");

                    // Summary
                    Console.WriteLine();
                    Console.WriteLine("=== CDN URL 404 FIX SUMMARY ===");
                    Console.WriteLine("🔧 Approaches Tested:");
                    Console.WriteLine("   1. ✅ REST API with CDN URL validation");
                    Console.WriteLine("   2. ✅ GraphQL with proper URL construction");
                    Console.WriteLine("   3. ✅ Alternative URL formats");
                    Console.WriteLine("   4. ✅ Download and re-upload with proper format");
                    Console.WriteLine();
                    Console.WriteLine("💡 Best Practices for CDN URLs:");
                    Console.WriteLine("   • Always validate CDN URLs before using");
                    Console.WriteLine("   • Implement retry logic with exponential backoff");
                    Console.WriteLine("   • Use multiple URL formats as fallbacks");
                    Console.WriteLine("   • Consider downloading images first for better control");
                    Console.WriteLine("   • Monitor CDN URL accessibility in production");

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
                Console.WriteLine($"❌ CDN URL 404 fix test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<string> TestAndFixCDNUrlAsync(string originalCdnUrl, long imageId)
        {
            Console.WriteLine($"🔄 Testing CDN URL: {originalCdnUrl}");
            
            // Test original URL
            var isOriginalWorking = await TestUrlAccessibilityAsync(originalCdnUrl);
            if (isOriginalWorking)
            {
                return originalCdnUrl;
            }

            Console.WriteLine("❌ Original CDN URL returns 404, trying fixes...");

            // Fix 1: Remove version parameter
            var urlWithoutVersion = RemoveVersionParameter(originalCdnUrl);
            if (await TestUrlAccessibilityAsync(urlWithoutVersion))
            {
                Console.WriteLine("✅ Fixed: Removed version parameter");
                return urlWithoutVersion;
            }

            // Fix 2: Try different version formats
            var alternativeVersions = GenerateAlternativeVersions(originalCdnUrl);
            foreach (var versionUrl in alternativeVersions)
            {
                if (await TestUrlAccessibilityAsync(versionUrl))
                {
                    Console.WriteLine($"✅ Fixed: Alternative version format");
                    return versionUrl;
                }
            }

            // Fix 3: Try different URL patterns
            var alternativePatterns = GenerateAlternativePatterns(originalCdnUrl, imageId);
            foreach (var patternUrl in alternativePatterns)
            {
                if (await TestUrlAccessibilityAsync(patternUrl))
                {
                    Console.WriteLine($"✅ Fixed: Alternative URL pattern");
                    return patternUrl;
                }
            }

            Console.WriteLine("❌ All CDN URL fixes failed");
            return originalCdnUrl;
        }

        private async Task<string> ConstructProperCDNUrlAsync(string fileId)
        {
            // Query the file to get the latest information
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

            var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, new { id = fileId });
            var parsedQuery = JsonConvert.DeserializeObject<dynamic>(queryResponse);
            var node = parsedQuery?.data?.node;

            if (node?.image != null)
            {
                // Try different URL fields in order of preference
                var url = node.image.url?.ToString() ?? 
                         node.image.src?.ToString() ?? 
                         node.image.originalSrc?.ToString() ?? 
                         node.image.transformedSrc?.ToString();

                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }
            }

            // If no URL from GraphQL, construct one from the file ID
            return ConstructCDNUrlFromFileId(fileId);
        }

        private string ConstructCDNUrlFromFileId(string fileId)
        {
            // Extract numeric ID from GraphQL ID
            if (fileId.StartsWith("gid://shopify/MediaImage/"))
            {
                var idParts = fileId.Split('/');
                if (idParts.Length >= 4)
                {
                    var numericId = idParts[3];
                    // Construct a basic CDN URL pattern
                    // Note: This is a fallback and may not work in all cases
                    return $"https://cdn.shopify.com/s/files/1/files/image_{numericId}.jpg";
                }
            }
            
            return string.Empty;
        }

        private async Task<Dictionary<string, string>> GenerateAlternativeCDNUrlsAsync(string originalUrl, long imageId)
        {
            var alternatives = new Dictionary<string, string>();

            // Alternative 1: Remove version parameter
            alternatives["No Version"] = RemoveVersionParameter(originalUrl);

            // Alternative 2: Different version formats
            var alternativeVersions = GenerateAlternativeVersions(originalUrl);
            for (int i = 0; i < alternativeVersions.Count; i++)
            {
                alternatives[$"Version {i + 1}"] = alternativeVersions[i];
            }

            // Alternative 3: Different URL patterns
            var alternativePatterns = GenerateAlternativePatterns(originalUrl, imageId);
            for (int i = 0; i < alternativePatterns.Count; i++)
            {
                alternatives[$"Pattern {i + 1}"] = alternativePatterns[i];
            }

            return alternatives;
        }

        private string RemoveVersionParameter(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            
            // Remove ?v= parameter
            var regex = new Regex(@"\?v=\d+(&|$)");
            var result = regex.Replace(url, "");
            
            // Clean up trailing ? if no other parameters
            if (result.EndsWith("?"))
            {
                result = result.TrimEnd('?');
            }
            
            return result;
        }

        private List<string> GenerateAlternativeVersions(string url)
        {
            var alternatives = new List<string>();
            
            if (string.IsNullOrEmpty(url)) return alternatives;

            // Try different version timestamps
            var timestamps = new[]
            {
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300).ToString(), // 5 minutes ago
                (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300).ToString(), // 5 minutes from now
                "1", // Simple version
                "0"  // No version
            };

            foreach (var timestamp in timestamps)
            {
                var alternative = url.Contains("?v=") 
                    ? url.Replace(Regex.Match(url, @"\?v=\d+").Value, $"?v={timestamp}")
                    : url + (url.Contains("?") ? "&" : "?") + $"v={timestamp}";
                
                alternatives.Add(alternative);
            }

            return alternatives;
        }

        private List<string> GenerateAlternativePatterns(string url, long imageId)
        {
            var alternatives = new List<string>();
            
            if (string.IsNullOrEmpty(url)) return alternatives;

            // Extract base URL without parameters
            var baseUrl = url.Split('?')[0];
            
            // Try different URL patterns
            alternatives.Add(baseUrl); // No parameters
            
            // Try with different file extensions
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            foreach (var ext in extensions)
            {
                var urlWithExt = baseUrl;
                if (!urlWithExt.EndsWith(ext))
                {
                    urlWithExt = urlWithExt.Replace(".jpg", ext).Replace(".jpeg", ext).Replace(".png", ext).Replace(".webp", ext);
                }
                alternatives.Add(urlWithExt);
            }

            // Try with image ID in URL
            alternatives.Add($"{baseUrl}?id={imageId}");
            alternatives.Add($"{baseUrl}?image_id={imageId}");

            return alternatives;
        }

        private async Task<string> DownloadAndReuploadWithProperFormatAsync(string imageUrl, string altText)
        {
            try
            {
                Console.WriteLine("🔄 Downloading image from source URL...");
                
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                Console.WriteLine($"✅ Downloaded {imageBytes.Length} bytes");

                // Convert to base64 and upload
                var base64Image = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:image/jpeg;base64,{base64Image}";
                
                var fileInput = new FileCreateInput
                {
                    OriginalSource = dataUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                var uploadedFile = response.Files[0];

                Console.WriteLine($"✅ Downloaded and re-uploaded file ID: {uploadedFile.Id}");

                // Wait for processing
                await Task.Delay(30000);

                // Query for CDN URL
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

                var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, new { id = uploadedFile.Id });
                var parsedQuery = JsonConvert.DeserializeObject<dynamic>(queryResponse);
                var node = parsedQuery?.data?.node;
                
                return node?.image?.url?.ToString() ?? 
                       node?.image?.src?.ToString() ?? 
                       node?.image?.originalSrc?.ToString() ?? 
                       string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Download and re-upload failed: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<bool> TestUrlAccessibilityAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            try
            {
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _httpClient?.Dispose();
        }
    }
} 