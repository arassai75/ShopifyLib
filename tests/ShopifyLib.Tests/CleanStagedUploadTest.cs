using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using ShopifyLib.Configuration;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class CleanStagedUploadTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public CleanStagedUploadTest()
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
        public async Task DownloadToMemory_StagedUpload_CreateFile_FollowingDocumentation()
        {
            Console.WriteLine("=== CLEAN STAGED UPLOAD TEST (Following Documentation) ===");
            Console.WriteLine("This test follows the exact Shopify documentation pattern:");
            Console.WriteLine("1. Download image to memory");
            Console.WriteLine("2. Create staged upload with image/jpeg");
            Console.WriteLine("3. Upload to staged URL");
            Console.WriteLine("4. Create file in Shopify");
            Console.WriteLine();

            byte[] imageBytes;

            // Step 1: Download from Indigo URL with User-Agent
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg";
            Console.WriteLine($"🔄 Step 1: Downloading image from {imageUrl}");
            try
            {
                using var downloadClient = new HttpClient();
                downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                downloadClient.Timeout = TimeSpan.FromSeconds(60);
                var response = await downloadClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                imageBytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"✅ Downloaded! Size: {imageBytes.Length} bytes");
                Console.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to download Indigo image: {ex.Message}");
                return;
            }
            
            Console.WriteLine($"✅ Loaded! Size: {imageBytes.Length} bytes");
            
            // Force JPEG format as per documentation
            var fileName = $"indigo-image-{DateTime.Now:yyyyMMdd-HHmmss}.jpg";
            var mimeType = "image/jpeg"; // Following documentation exactly
            
            Console.WriteLine($"📄 Using filename: {fileName}");
            Console.WriteLine($"📄 Using MIME type: {mimeType}");
            Console.WriteLine();

            // Step 2: Create staged upload (following documentation exactly)
            Console.WriteLine("🔄 Step 2: Creating staged upload...");
            
            var stagedUploadInput = new
            {
                filename = fileName,
                mimeType = mimeType,
                httpMethod = "PUT", // Use PUT as we discovered
                resource = "FILE" // Use FILE resource type
            };

            var stagedUploadResponse = await _client.GraphQL.ExecuteQueryAsync(@"
                mutation stagedUploadsCreate($input: [StagedUploadInput!]!) {
                    stagedUploadsCreate(input: $input) {
                        stagedTargets {
                            url
                            resourceUrl
                            parameters {
                                name
                                value
                            }
                        }
                        userErrors {
                            field
                            message
                        }
                    }
                }", new { input = new[] { stagedUploadInput } });

            var stagedData = JsonConvert.DeserializeObject<dynamic>(stagedUploadResponse);
            
            if (stagedData.data?.stagedUploadsCreate?.userErrors != null && stagedData.data.stagedUploadsCreate.userErrors.Count > 0)
            {
                Console.WriteLine("❌ Staged upload creation failed:");
                foreach (var error in stagedData.data.stagedUploadsCreate.userErrors)
                {
                    Console.WriteLine($"   - {error.message}");
                }
                return;
            }

            var stagedTarget = stagedData.data?.stagedUploadsCreate?.stagedTargets?[0];
            if (stagedTarget == null)
            {
                Console.WriteLine("❌ No staged target returned");
                return;
            }

            Console.WriteLine($"✅ Staged upload created!");
            Console.WriteLine($"URL: {stagedTarget.url}");
            Console.WriteLine($"Resource URL: {stagedTarget.resourceUrl}");
            Console.WriteLine("Parameters:");
            foreach (var param in stagedTarget.parameters)
            {
                Console.WriteLine($"   {param.name} = {param.value}");
            }
            Console.WriteLine();

            // Step 3: Upload to staged URL
            Console.WriteLine("🔄 Step 3: Uploading to staged URL...");
            
            using var uploadClient = new HttpClient();
            uploadClient.Timeout = TimeSpan.FromSeconds(60);

            // Create multipart form data exactly as required
            var boundary = "----WebKitFormBoundary" + Guid.NewGuid().ToString("N").Substring(0, 16);
            var utf8NoBom = new System.Text.UTF8Encoding(false);

            using var ms = new MemoryStream();

            // Add parameters first
            foreach (var param in stagedTarget.parameters)
            {
                var paramBoundary = utf8NoBom.GetBytes($"--{boundary}\r\n");
                ms.Write(paramBoundary, 0, paramBoundary.Length);
                var paramHeader = utf8NoBom.GetBytes($"Content-Disposition: form-data; name=\"{param.name}\"\r\n\r\n");
                ms.Write(paramHeader, 0, paramHeader.Length);
                var paramValue = utf8NoBom.GetBytes($"{param.value}\r\n");
                ms.Write(paramValue, 0, paramValue.Length);
            }

            // Add file
            var fileBoundary = utf8NoBom.GetBytes($"--{boundary}\r\n");
            ms.Write(fileBoundary, 0, fileBoundary.Length);
            var fileHeader = utf8NoBom.GetBytes($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n");
            ms.Write(fileHeader, 0, fileHeader.Length);
            var fileContentType = utf8NoBom.GetBytes($"Content-Type: {mimeType}\r\n\r\n");
            ms.Write(fileContentType, 0, fileContentType.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            var closing = utf8NoBom.GetBytes($"\r\n--{boundary}--\r\n");
            ms.Write(closing, 0, closing.Length);

            var formData = ms.ToArray();

            // Upload using PUT method
            using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, stagedTarget.url.ToString());
            uploadRequest.Content = new ByteArrayContent(formData);
            uploadRequest.Content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

            var uploadResponse = await uploadClient.SendAsync(uploadRequest);
            
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorBody = await uploadResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Upload failed: {uploadResponse.StatusCode}");
                Console.WriteLine($"Error: {errorBody}");
                return;
            }

            Console.WriteLine($"✅ Upload successful! Status: {uploadResponse.StatusCode}");
            Console.WriteLine();

            // Step 4: Create file in Shopify
            Console.WriteLine("🔄 Step 4: Creating file in Shopify...");
            
            var fileCreateInput = new FileCreateInput
            {
                OriginalSource = stagedTarget.resourceUrl.ToString(),
                ContentType = "IMAGE",
                Alt = "Indigo image - Clean staged upload test"
            };

            var fileCreateResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileCreateInput });
            
            if (fileCreateResponse.UserErrors != null && fileCreateResponse.UserErrors.Count > 0)
            {
                Console.WriteLine("❌ File creation failed:");
                foreach (var error in fileCreateResponse.UserErrors)
                {
                    Console.WriteLine($"   - {error.Message}");
                }
                return;
            }

            if (fileCreateResponse.Files == null || fileCreateResponse.Files.Count == 0)
            {
                Console.WriteLine("❌ No file returned from creation");
                return;
            }

            var createdFile = fileCreateResponse.Files[0];
            Console.WriteLine($"🎉 File created successfully!");
            Console.WriteLine($"File ID: {createdFile.Id}");
            Console.WriteLine($"Status: {createdFile.FileStatus}");
            Console.WriteLine($"Alt Text: {createdFile.Alt}");
            Console.WriteLine($"Created: {createdFile.CreatedAt}");
            
            if (createdFile.Image != null)
            {
                Console.WriteLine($"Image URL: {createdFile.Image.Url ?? "Not available"}");
                Console.WriteLine($"Dimensions: {createdFile.Image.Width}x{createdFile.Image.Height}");
            }

            Console.WriteLine();
            Console.WriteLine("✅ Complete flow successful!");
            Console.WriteLine("📋 Check your Shopify dashboard under Content > Files");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private byte[] CreateSimpleJpeg()
        {
            // Create a minimal 1x1 JPEG image
            // This is a valid 1x1 JPEG with minimal header
            return new byte[]
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
                0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
                0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
                0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
                0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
                0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
                0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
                0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01,
                0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0xFF, 0xC4,
                0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x0C,
                0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00, 0x8A, 0x00,
                0xFF, 0xD9
            };
        }
    }
} 