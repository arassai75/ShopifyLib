using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Services;
using ShopifyLib.Configuration;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp
{
    /// <summary>
    /// Example demonstrating local image upload capabilities
    /// </summary>
    public class LocalImageUploadExample
    {
        private readonly ShopifyClient _client;
        private readonly LocalImageUploadService _localImageService;

        public LocalImageUploadExample()
        {
            // Load configuration
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
            _localImageService = new LocalImageUploadService(_client.Files);
        }

        public async Task RunExample()
        {
            Console.WriteLine("=== SHOPIFY LOCAL IMAGE UPLOAD EXAMPLE ===");
            Console.WriteLine("This example demonstrates uploading local images to Shopify Media space");
            Console.WriteLine();

            try
            {
                // Example 1: Upload a single local image
                await DemonstrateSingleImageUpload();

                // Example 2: Upload multiple local images
                await DemonstrateMultipleImageUpload();

                // Example 3: Upload images from a directory
                await DemonstrateDirectoryUpload();

                // Example 4: Get image information
                await DemonstrateImageInfo();

                Console.WriteLine();
                Console.WriteLine("✅ Local image upload example completed successfully!");
                Console.WriteLine();
                Console.WriteLine("💡 Tips:");
                Console.WriteLine("   • Supported formats: JPG, JPEG, PNG, GIF, WebP");
                Console.WriteLine("   • Images are converted to base64 for upload");
                Console.WriteLine("   • File size limits apply (typically 20MB per file)");
                Console.WriteLine("   • Use descriptive alt text for accessibility");
                Console.WriteLine("   • Consider image optimization before upload");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Example failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
            }
        }

        private async Task DemonstrateSingleImageUpload()
        {
            Console.WriteLine("📁 Example 1: Single Local Image Upload");
            Console.WriteLine("========================================");

            // Create a sample image path (you would use your actual image path)
            var sampleImagePath = await CreateSampleImage("sample-product.jpg", 800, 600);

            try
            {
                Console.WriteLine($"📤 Uploading: {sampleImagePath}");
                
                var response = await _localImageService.UploadLocalImageAsync(
                    sampleImagePath, 
                    "Sample Product Image"
                );

                var uploadedFile = response.Files[0];
                Console.WriteLine($"✅ Upload successful!");
                Console.WriteLine($"   File ID: {uploadedFile.Id}");
                Console.WriteLine($"   Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"   Alt Text: {uploadedFile.Alt}");

                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"   Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"   CDN URL: {uploadedFile.Image.Src}");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Single image upload failed: {ex.Message}");
            }
            finally
            {
                // Cleanup test file
                if (System.IO.File.Exists(sampleImagePath))
                    System.IO.File.Delete(sampleImagePath);
            }
        }

        private async Task DemonstrateMultipleImageUpload()
        {
            Console.WriteLine("📁 Example 2: Multiple Local Images Upload");
            Console.WriteLine("===========================================");

            // Create sample images
            var sampleImages = new List<string>
            {
                await CreateSampleImage("product1.jpg", 800, 600),
                await CreateSampleImage("product2.png", 1200, 800),
                await CreateSampleImage("banner.gif", 1920, 400)
            };

            var filePaths = new List<(string FilePath, string AltText)>
            {
                (sampleImages[0], "Product Image 1 - Red Shirt"),
                (sampleImages[1], "Product Image 2 - Blue Jeans"),
                (sampleImages[2], "Banner Image - Summer Sale")
            };

            try
            {
                Console.WriteLine($"📤 Uploading {sampleImages.Count} images...");
                
                var response = await _localImageService.UploadLocalImagesAsync(filePaths);

                Console.WriteLine($"✅ Upload successful! {response.Files.Count} files uploaded:");
                Console.WriteLine();

                foreach (var file in response.Files)
                {
                    Console.WriteLine($"   📁 {file.Alt}");
                    Console.WriteLine($"      ID: {file.Id}");
                    Console.WriteLine($"      Status: {file.FileStatus}");
                    
                    if (file.Image != null)
                    {
                        Console.WriteLine($"      Dimensions: {file.Image.Width}x{file.Image.Height}");
                        Console.WriteLine($"      CDN URL: {file.Image.Src}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Multiple image upload failed: {ex.Message}");
            }
            finally
            {
                // Cleanup test files
                foreach (var imagePath in sampleImages)
                {
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }
            }
        }

        private async Task DemonstrateDirectoryUpload()
        {
            Console.WriteLine("📁 Example 3: Directory Upload");
            Console.WriteLine("==============================");

            // Create a temporary directory with sample images
            var tempDir = Path.Combine(Path.GetTempPath(), "ShopifyLibExample");
            Directory.CreateDirectory(tempDir);

            var sampleImages = new List<string>
            {
                await CreateSampleImage(Path.Combine(tempDir, "product1.jpg"), 800, 600),
                await CreateSampleImage(Path.Combine(tempDir, "product2.png"), 1200, 800),
                await CreateSampleImage(Path.Combine(tempDir, "banner.gif"), 1920, 400)
            };

            try
            {
                Console.WriteLine($"📤 Uploading all images from directory: {tempDir}");
                
                var response = await _localImageService.UploadImagesFromDirectoryAsync(
                    tempDir,
                    "*.jpg;*.png;*.gif",
                    "Product Image"
                );

                Console.WriteLine($"✅ Directory upload successful! {response.Files.Count} files uploaded:");
                Console.WriteLine();

                foreach (var file in response.Files)
                {
                    Console.WriteLine($"   📁 {file.Alt}");
                    Console.WriteLine($"      ID: {file.Id}");
                    Console.WriteLine($"      Status: {file.FileStatus}");
                    
                    if (file.Image != null)
                    {
                        Console.WriteLine($"      Dimensions: {file.Image.Width}x{file.Image.Height}");
                        Console.WriteLine($"      CDN URL: {file.Image.Src}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Directory upload failed: {ex.Message}");
            }
            finally
            {
                // Cleanup test files
                foreach (var imagePath in sampleImages)
                {
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }
                
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        private async Task DemonstrateImageInfo()
        {
            Console.WriteLine("📁 Example 4: Image Information");
            Console.WriteLine("===============================");

            var sampleImagePath = await CreateSampleImage("info-sample.jpg", 1024, 768);

            try
            {
                Console.WriteLine($"📊 Getting information for: {sampleImagePath}");
                
                var imageInfo = await _localImageService.GetImageInfoAsync(sampleImagePath);

                Console.WriteLine("📊 Image Information:");
                Console.WriteLine($"   File Name: {imageInfo.FileName}");
                Console.WriteLine($"   File Size: {imageInfo.FileSize:N0} bytes ({imageInfo.FileSizeInMB:F2} MB)");
                Console.WriteLine($"   Extension: {imageInfo.Extension}");
                Console.WriteLine($"   Content Type: {imageInfo.ContentType}");
                Console.WriteLine($"   Is Valid Image: {imageInfo.IsValidImage}");
                Console.WriteLine($"   Created: {imageInfo.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Modified: {imageInfo.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Image info failed: {ex.Message}");
            }
            finally
            {
                // Cleanup test file
                if (System.IO.File.Exists(sampleImagePath))
                    System.IO.File.Delete(sampleImagePath);
            }
        }

        /// <summary>
        /// Creates a sample image file for demonstration purposes
        /// </summary>
        /// <param name="fileName">Name of the file to create</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Path to the created sample image</returns>
        private async Task<string> CreateSampleImage(string fileName, int width, int height)
        {
            var filePath = fileName;
            
            // Create a simple test image (this is a minimal valid JPEG)
            var testImageBytes = CreateMinimalJpeg(width, height);
            await System.IO.File.WriteAllBytesAsync(filePath, testImageBytes);
            
            return filePath;
        }

        /// <summary>
        /// Creates a minimal valid JPEG image for demonstration
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>JPEG bytes</returns>
        private byte[] CreateMinimalJpeg(int width, int height)
        {
            // This is a minimal valid JPEG structure for demonstration
            // In a real scenario, you'd use actual image files
            var jpegHeader = new byte[]
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00
            };

            var jpegFooter = new byte[] { 0xFF, 0xD9 };

            // Create simple color data
            var colorData = new byte[width * height * 3];
            for (int i = 0; i < colorData.Length; i += 3)
            {
                colorData[i] = 255;     // Red
                colorData[i + 1] = 0;   // Green
                colorData[i + 2] = 0;   // Blue
            }

            var result = new byte[jpegHeader.Length + colorData.Length + jpegFooter.Length];
            Array.Copy(jpegHeader, 0, result, 0, jpegHeader.Length);
            Array.Copy(colorData, 0, result, jpegHeader.Length, colorData.Length);
            Array.Copy(jpegFooter, 0, result, jpegHeader.Length + colorData.Length, jpegFooter.Length);

            return result;
        }
    }
} 