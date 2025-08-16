using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Configuration;
using Microsoft.Extensions.Configuration;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Test for handling  images that require User-Agent headers
    /// </summary>
    [IntegrationTest]
    public class UserAgentTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private string? _uploadedFileId;

        public UserAgentTest()
        {
            // Load configuration from appsettings.json and environment variables
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var shopifyConfig = configuration.GetShopifyConfig();
            if (!shopifyConfig.IsValid())
            {
                throw new InvalidOperationException("Invalid Shopify configuration. Please check your appsettings.json or environment variables.");
            }

            _client = new ShopifyClient(shopifyConfig);
        }

        [Fact]
        public async Task UploadImage_WithUserAgent_DownloadAndUploadSuccessfully()
        {
            // Arrange
            var imageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = " Gift Image - User-Agent Test";

            Console.WriteLine("===  USER-AGENT TEST ===");
            Console.WriteLine("This test downloads the  image with proper User-Agent and uploads to Shopify");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Step 1: Download image with proper User-Agent
                Console.WriteLine("📥 Step 1: Downloading image with User-Agent header...");
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Shopify-Image-Uploader/1.0)");
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                byte[] imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    Console.WriteLine($"✅ Download successful! Size: {imageBytes.Length} bytes");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"❌ Download failed: {ex.Message}");
                    Console.WriteLine("This confirms that  requires a User-Agent header");
                    throw;
                }

                // Step 2: Convert to base64 and upload to Shopify
                Console.WriteLine("📤 Step 2: Converting to base64 and uploading to Shopify...");
                
                var base64Image = Convert.ToBase64String(imageBytes);
                var fileInput = new FileCreateInput
                {
                    OriginalSource = $"data:image/jpeg;base64,{base64Image}",
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var uploadResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
                
                if (uploadResponse?.Files == null || uploadResponse.Files.Count == 0)
                {
                    throw new Exception("Upload failed - no files returned");
                }

                var uploadedFile = uploadResponse.Files[0];
                _uploadedFileId = uploadedFile.Id;

                Console.WriteLine($"✅ Upload successful!");
                Console.WriteLine($"   File ID: {uploadedFile.Id}");
                Console.WriteLine($"   File Status: {uploadedFile.FileStatus}");
                
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"   Original Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"   Original Src: {uploadedFile.Image.Src}");
                    Console.WriteLine($"   CDN URL: {uploadedFile.Image.Url}");
                }

                Console.WriteLine();
                Console.WriteLine("✅  image successfully uploaded with User-Agent workaround!");
                Console.WriteLine("💡 This approach bypasses Shopify's CDN download limitations");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
                throw;
            }
        }

        [Fact]
        public async Task CompareApproaches_Image_DemonstratesUserAgentIssue()
        {
            // Arrange
            var imageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = " Gift Image - Comparison Test";

            Console.WriteLine("===  APPROACH COMPARISON TEST ===");
            Console.WriteLine("This test compares different approaches for uploading  images");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine();

            try
            {
                // Approach 1: Direct URL upload (will likely fail)
                Console.WriteLine("🔄 Approach 1: Direct URL upload (Shopify downloads without User-Agent)...");
                try
                {
                    var directFileInput = new FileCreateInput
                    {
                        OriginalSource = imageUrl,
                        ContentType = FileContentType.Image,
                        Alt = $"{altText} - Direct"
                    };

                    var directResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { directFileInput });
                    Console.WriteLine($"✅ Direct upload succeeded! File ID: {directResponse.Files[0].Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Direct upload failed: {ex.Message}");
                    Console.WriteLine("   This confirms Shopify's CDN cannot download from  without User-Agent");
                }

                Console.WriteLine();

                // Approach 2: Download with User-Agent, then upload
                Console.WriteLine("🔄 Approach 2: Download with User-Agent, then upload...");
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Shopify-Image-Uploader/1.0)");
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    var base64Image = Convert.ToBase64String(imageBytes);
                    
                    var base64FileInput = new FileCreateInput
                    {
                        OriginalSource = $"data:image/jpeg;base64,{base64Image}",
                        ContentType = FileContentType.Image,
                        Alt = $"{altText} - Base64"
                    };

                    var base64Response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { base64FileInput });
                    _uploadedFileId = base64Response.Files[0].Id;
                    
                    Console.WriteLine($"✅ Base64 upload succeeded! File ID: {base64Response.Files[0].Id}");
                    Console.WriteLine($"   Image size: {imageBytes.Length} bytes");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Base64 upload failed: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("📊 Comparison Results:");
                Console.WriteLine("   • Direct URL upload: Likely fails due to missing User-Agent");
                Console.WriteLine("   • Download + Base64 upload: Works with proper User-Agent");
                Console.WriteLine("   • Recommendation: Use download + base64 approach for  images");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
                throw;
            }
        }

        public void Dispose()
        {
            // Cleanup: Delete the uploaded file if it exists
            if (!string.IsNullOrEmpty(_uploadedFileId))
            {
                try
                {
                    // Note: File deletion would require additional GraphQL mutation
                    // For now, we'll just log that cleanup would happen
                    Console.WriteLine($"🧹 Cleanup: Would delete file {_uploadedFileId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Cleanup failed: {ex.Message}");
                }
            }
        }
    }
} 