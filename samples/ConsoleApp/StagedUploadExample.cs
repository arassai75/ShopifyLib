using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Services;
using Newtonsoft.Json;

namespace ConsoleApp
{
    /// <summary>
    /// Example demonstrating staged upload functionality
    /// This approach can bypass URL download issues by uploading files directly to Shopify
    /// </summary>
    public class StagedUploadExample
    {
        private readonly ShopifyClient _client;
        private readonly StagedUploadService _stagedUploadService;

        public StagedUploadExample(ShopifyClient client)
        {
            _client = client;
            _stagedUploadService = new StagedUploadService(_client.GraphQL, _client.HttpClient);
        }

        /// <summary>
        /// Demonstrates staged upload for problematic URLs like Indigo
        /// </summary>
        public async Task RunStagedUploadExample()
        {
            Console.WriteLine("=== STAGED UPLOAD EXAMPLE ===");
            Console.WriteLine("This example demonstrates how staged upload can bypass URL download issues");
            Console.WriteLine("It's particularly useful for URLs that require User-Agent headers or have other restrictions");
            Console.WriteLine();

            try
            {
                // Example 1: Upload Indigo image using staged upload
                await UploadIndigoImageWithStagedUpload();

                Console.WriteLine();

                // Example 2: Upload multiple files using staged upload
                await UploadMultipleFilesWithStagedUpload();

                Console.WriteLine();

                // Example 3: Compare staged upload vs direct upload
                await CompareStagedUploadVsDirectUpload();

                Console.WriteLine();
                Console.WriteLine("🎉 All staged upload examples completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Example failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
            }
        }

        /// <summary>
        /// Uploads an Indigo image using staged upload approach
        /// </summary>
        private async Task UploadIndigoImageWithStagedUpload()
        {
            Console.WriteLine("📸 Example 1: Uploading Indigo Image with Staged Upload");
            Console.WriteLine("This demonstrates how to handle URLs that require User-Agent headers");
            Console.WriteLine();

            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var fileName = "indigo-gift-image.jpg";
            var contentType = "image/jpeg";
            var altText = "Indigo Gift Image - Staged Upload Example";
            var userAgent = "Mozilla/5.0 (compatible; Shopify-Image-Uploader/1.0)";

            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Filename: {fileName}");
            Console.WriteLine($"Content Type: {contentType}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine($"User-Agent: {userAgent}");
            Console.WriteLine();

            try
            {
                // Upload using staged upload from URL
                Console.WriteLine("🔄 Uploading using staged upload from URL...");
                
                var uploadResponse = await _stagedUploadService.UploadFileFromUrlAsync(
                    imageUrl, 
                    fileName, 
                    contentType, 
                    altText,
                    userAgent
                );
                
                if (uploadResponse?.Files == null || uploadResponse.Files.Count == 0)
                {
                    throw new Exception("Upload failed - no files returned");
                }

                var uploadedFile = uploadResponse.Files[0];

                Console.WriteLine("✅ Staged upload successful!");
                Console.WriteLine();
                Console.WriteLine("=== UPLOADED FILE DETAILS ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");
                
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"📏 Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"🌐 CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"🔗 Original Src: {uploadedFile.Image.Src ?? "Not available"}");
                }

                Console.WriteLine();
                Console.WriteLine("💡 Key Benefits of Staged Upload:");
                Console.WriteLine("   • Bypasses Shopify's CDN download limitations");
                Console.WriteLine("   • Allows custom User-Agent headers for problematic URLs");
                Console.WriteLine("   • Uploads file directly to Shopify's servers");
                Console.WriteLine("   • Provides immediate access to CDN URLs");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Staged upload failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Uploads multiple files using staged upload
        /// </summary>
        private async Task UploadMultipleFilesWithStagedUpload()
        {
            Console.WriteLine("📁 Example 2: Uploading Multiple Files with Staged Upload");
            Console.WriteLine("This demonstrates batch file upload using staged upload");
            Console.WriteLine();

            // Create sample files
            var files = new List<(byte[] Bytes, string FileName, string ContentType, string? AltText)>
            {
                (System.Text.Encoding.UTF8.GetBytes("This is the content of file 1."), "document1.txt", "text/plain", "Sample Document 1"),
                (System.Text.Encoding.UTF8.GetBytes("This is the content of file 2."), "document2.txt", "text/plain", "Sample Document 2"),
                (System.Text.Encoding.UTF8.GetBytes("{\"name\":\"sample\",\"type\":\"json\"}"), "data.json", "application/json", "Sample JSON Data")
            };

            Console.WriteLine($"Number of files to upload: {files.Count}");
            foreach (var (_, fileName, contentType, altText) in files)
            {
                Console.WriteLine($"   • {fileName} ({contentType}) - {altText}");
            }
            Console.WriteLine();

            try
            {
                // Upload multiple files using staged upload
                Console.WriteLine("🔄 Uploading multiple files using staged upload...");
                
                var uploadResponse = await _stagedUploadService.UploadFilesAsync(files);
                
                if (uploadResponse?.Files == null || uploadResponse.Files.Count == 0)
                {
                    throw new Exception("Upload failed - no files returned");
                }

                Console.WriteLine("✅ Multiple files staged upload successful!");
                Console.WriteLine();
                Console.WriteLine("=== UPLOADED FILES DETAILS ===");
                
                foreach (var file in uploadResponse.Files)
                {
                    Console.WriteLine($"📁 File ID: {file.Id}");
                    Console.WriteLine($"📊 Status: {file.FileStatus}");
                    Console.WriteLine($"📝 Alt: {file.Alt ?? "Not set"}");
                    Console.WriteLine($"📅 Created: {file.CreatedAt}");
                    Console.WriteLine();
                }

                Console.WriteLine("💡 Batch Upload Benefits:");
                Console.WriteLine("   • Efficient handling of multiple files");
                Console.WriteLine("   • Consistent upload process for all files");
                Console.WriteLine("   • Better error handling and reporting");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Multiple files upload failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Compares staged upload vs direct upload approaches
        /// </summary>
        private async Task CompareStagedUploadVsDirectUpload()
        {
            Console.WriteLine("🔄 Example 3: Comparing Staged Upload vs Direct Upload");
            Console.WriteLine("This demonstrates the differences between the two approaches");
            Console.WriteLine();

            var imageUrl = "https://httpbin.org/image/jpeg"; // Using a reliable test URL
            var altText = "Comparison Test Image";

            Console.WriteLine($"Test Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            // Approach 1: Direct URL upload
            Console.WriteLine("🔄 Approach 1: Direct URL Upload");
            Console.WriteLine("Using GraphQL fileCreate mutation with URL directly");
            
            try
            {
                var directFileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = $"{altText} - Direct"
                };

                var directResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { directFileInput });
                var directFile = directResponse.Files[0];

                Console.WriteLine("✅ Direct upload successful!");
                Console.WriteLine($"   File ID: {directFile.Id}");
                Console.WriteLine($"   Status: {directFile.FileStatus}");
                Console.WriteLine($"   CDN URL: {directFile.Image?.Url ?? "Not available"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Direct upload failed: {ex.Message}");
            }

            Console.WriteLine();

            // Approach 2: Staged upload
            Console.WriteLine("🔄 Approach 2: Staged Upload");
            Console.WriteLine("Using stagedUploadsCreate + direct file upload");
            
            try
            {
                var stagedResponse = await _stagedUploadService.UploadFileFromUrlAsync(
                    imageUrl,
                    "comparison-test-staged.jpg",
                    "image/jpeg",
                    $"{altText} - Staged"
                );
                
                var stagedFile = stagedResponse.Files[0];

                Console.WriteLine("✅ Staged upload successful!");
                Console.WriteLine($"   File ID: {stagedFile.Id}");
                Console.WriteLine($"   Status: {stagedFile.FileStatus}");
                Console.WriteLine($"   CDN URL: {stagedFile.Image?.Url ?? "Not available"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Staged upload failed: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("📊 Comparison Summary:");
            Console.WriteLine("   Direct Upload:");
            Console.WriteLine("     • Simpler API call");
            Console.WriteLine("     • Shopify handles the download");
            Console.WriteLine("     • May fail with problematic URLs");
            Console.WriteLine("     • No control over download process");
            Console.WriteLine();
            Console.WriteLine("   Staged Upload:");
            Console.WriteLine("     • More complex but more control");
            Console.WriteLine("     • You handle the download");
            Console.WriteLine("     • Can handle problematic URLs");
            Console.WriteLine("     • Custom headers and error handling");
            Console.WriteLine("     • Direct file upload to Shopify");
        }

        /// <summary>
        /// Demonstrates uploading a file from bytes using staged upload
        /// </summary>
        public async Task UploadFileFromBytesExample()
        {
            Console.WriteLine("📄 Example: Uploading File from Bytes with Staged Upload");
            Console.WriteLine("This demonstrates uploading file data directly without downloading from URL");
            Console.WriteLine();

            // Create sample file content
            var fileContent = "This is a sample text file created for demonstration purposes.\nIt contains multiple lines of text.\nThis file will be uploaded using staged upload.";
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
            var fileName = "sample-text-file.txt";
            var contentType = "text/plain";
            var altText = "Sample Text File - Staged Upload";

            Console.WriteLine($"Filename: {fileName}");
            Console.WriteLine($"Content Type: {contentType}");
            Console.WriteLine($"File Size: {fileBytes.Length} bytes");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Upload using staged upload
                Console.WriteLine("🔄 Uploading file from bytes using staged upload...");
                
                var uploadResponse = await _stagedUploadService.UploadFileAsync(
                    fileBytes, 
                    fileName, 
                    contentType, 
                    altText
                );
                
                if (uploadResponse?.Files == null || uploadResponse.Files.Count == 0)
                {
                    throw new Exception("Upload failed - no files returned");
                }

                var uploadedFile = uploadResponse.Files[0];

                Console.WriteLine("✅ File upload from bytes successful!");
                Console.WriteLine();
                Console.WriteLine("=== UPLOADED FILE DETAILS ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");

                Console.WriteLine();
                Console.WriteLine("💡 Benefits of Uploading from Bytes:");
                Console.WriteLine("   • No external URL dependencies");
                Console.WriteLine("   • Complete control over file content");
                Console.WriteLine("   • Can handle generated or modified files");
                Console.WriteLine("   • Faster upload process");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ File upload from bytes failed: {ex.Message}");
                throw;
            }
        }
    }
} 