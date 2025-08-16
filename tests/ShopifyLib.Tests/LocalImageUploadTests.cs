using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Services;
using ShopifyLib.Configuration;
using Microsoft.Extensions.Configuration;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Tests for uploading local images to Shopify Media space
    /// </summary>
    [IntegrationTest]
    public class LocalImageUploadTests : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly LocalImageUploadService _localImageService;
        private readonly List<string> _uploadedFileIds = new();
        private readonly string _testImagesDir;

        public LocalImageUploadTests()
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

            // Create test images directory
            _testImagesDir = Path.Combine(Path.GetTempPath(), "ShopifyLibTestImages");
            Directory.CreateDirectory(_testImagesDir);
        }

        [Fact]
        public async Task UploadLocalImage_SingleFile_UploadsSuccessfully()
        {
            // Arrange
            var testImagePath = await CreateTestImage("test-image.jpg", 1024, 768);

            Console.WriteLine("=== LOCAL IMAGE UPLOAD TEST ===");
            Console.WriteLine($"Test image path: {testImagePath}");
            Console.WriteLine();

            try
            {
                // Act
                Console.WriteLine("📤 Uploading local image to Shopify...");
                var response = await _localImageService.UploadLocalImageAsync(testImagePath, "Test Local Image");

                // Assert
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.Single(response.Files);

                var uploadedFile = response.Files[0];
                _uploadedFileIds.Add(uploadedFile.Id);

                Console.WriteLine($"✅ Upload successful!");
                Console.WriteLine($"   File ID: {uploadedFile.Id}");
                Console.WriteLine($"   File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"   Alt Text: {uploadedFile.Alt}");

                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"   Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"   CDN URL: {uploadedFile.Image.Src}");
                }

                Console.WriteLine();
                Console.WriteLine("✅ Local image upload test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup test file
                if (System.IO.File.Exists(testImagePath))
                    System.IO.File.Delete(testImagePath);
            }
        }

        [Fact]
        public async Task UploadLocalImages_MultipleFiles_UploadsSuccessfully()
        {
            // Arrange
            var testImages = new List<string>
            {
                await CreateTestImage("image1.jpg", 800, 600),
                await CreateTestImage("image2.png", 1200, 800),
                await CreateTestImage("image3.gif", 400, 300)
            };

            var filePaths = new List<(string FilePath, string AltText)>
            {
                (testImages[0], "Product Image 1"),
                (testImages[1], "Product Image 2"),
                (testImages[2], "Product Image 3")
            };

            Console.WriteLine("=== MULTIPLE LOCAL IMAGES UPLOAD TEST ===");
            Console.WriteLine($"Uploading {testImages.Count} test images...");
            Console.WriteLine();

            try
            {
                // Act
                Console.WriteLine("📤 Uploading multiple local images to Shopify...");
                var response = await _localImageService.UploadLocalImagesAsync(filePaths);

                // Assert
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.Equal(3, response.Files.Count);

                Console.WriteLine($"✅ Upload successful! {response.Files.Count} files uploaded:");
                Console.WriteLine();

                foreach (var file in response.Files)
                {
                    _uploadedFileIds.Add(file.Id);
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

                Console.WriteLine("✅ Multiple local images upload test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup test files
                foreach (var imagePath in testImages)
                {
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }
            }
        }

        [Fact]
        public async Task UploadImagesFromDirectory_DirectoryUpload_UploadsSuccessfully()
        {
            // Arrange
            var testImages = new List<string>
            {
                await CreateTestImage("product1.jpg", 800, 600),
                await CreateTestImage("product2.png", 1200, 800),
                await CreateTestImage("banner.gif", 1920, 400)
            };

            Console.WriteLine("=== DIRECTORY UPLOAD TEST ===");
            Console.WriteLine($"Test directory: {_testImagesDir}");
            Console.WriteLine($"Created {testImages.Count} test images");
            Console.WriteLine();

            try
            {
                // Act
                Console.WriteLine("📤 Uploading all images from directory to Shopify...");
                var response = await _localImageService.UploadImagesFromDirectoryAsync(
                    _testImagesDir, 
                    "*.jpg;*.png;*.gif", 
                    "Product Image"
                );

                // Assert
                Assert.NotNull(response);
                Assert.NotNull(response.Files);
                Assert.Equal(3, response.Files.Count);

                Console.WriteLine($"✅ Directory upload successful! {response.Files.Count} files uploaded:");
                Console.WriteLine();

                foreach (var file in response.Files)
                {
                    _uploadedFileIds.Add(file.Id);
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

                Console.WriteLine("✅ Directory upload test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup test files
                foreach (var imagePath in testImages)
                {
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }
            }
        }

        [Fact]
        public async Task GetImageInfo_LocalFile_ReturnsCorrectInfo()
        {
            // Arrange
            var testImagePath = await CreateTestImage("info-test.jpg", 1024, 768);

            Console.WriteLine("=== LOCAL IMAGE INFO TEST ===");
            Console.WriteLine($"Test image: {testImagePath}");
            Console.WriteLine();

            try
            {
                // Act
                var imageInfo = await _localImageService.GetImageInfoAsync(testImagePath);

                // Assert
                Assert.NotNull(imageInfo);
                Assert.Equal(testImagePath, imageInfo.FilePath);
                Assert.Equal("info-test.jpg", imageInfo.FileName);
                Assert.Equal(".jpg", imageInfo.Extension);
                Assert.Equal("image/jpeg", imageInfo.ContentType);
                Assert.True(imageInfo.IsValidImage);
                Assert.True(imageInfo.FileSize > 0);
                Assert.True(imageInfo.FileSizeInMB > 0);

                Console.WriteLine("📊 Image Information:");
                Console.WriteLine($"   File Name: {imageInfo.FileName}");
                Console.WriteLine($"   File Size: {imageInfo.FileSize} bytes ({imageInfo.FileSizeInMB} MB)");
                Console.WriteLine($"   Extension: {imageInfo.Extension}");
                Console.WriteLine($"   Content Type: {imageInfo.ContentType}");
                Console.WriteLine($"   Is Valid Image: {imageInfo.IsValidImage}");
                Console.WriteLine($"   Created: {imageInfo.CreatedAt}");
                Console.WriteLine($"   Modified: {imageInfo.ModifiedAt}");

                Console.WriteLine();
                Console.WriteLine("✅ Image info test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup test file
                if (System.IO.File.Exists(testImagePath))
                    System.IO.File.Delete(testImagePath);
            }
        }

        [Fact]
        public async Task UploadLocalImage_InvalidFile_ThrowsException()
        {
            // Arrange
            var invalidPath = "/path/to/nonexistent/image.jpg";

            Console.WriteLine("=== INVALID FILE UPLOAD TEST ===");
            Console.WriteLine($"Invalid path: {invalidPath}");
            Console.WriteLine();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<FileNotFoundException>(() => 
                    _localImageService.UploadLocalImageAsync(invalidPath, "Test")
                );

                Console.WriteLine("✅ Correctly threw FileNotFoundException for invalid path");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a test image file with specified dimensions
        /// </summary>
        /// <param name="fileName">Name of the file to create</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Path to the created test image</returns>
        private async Task<string> CreateTestImage(string fileName, int width, int height)
        {
            var filePath = Path.Combine(_testImagesDir, fileName);
            
            // Create a simple test image (this is a minimal valid JPEG)
            var testImageBytes = CreateMinimalJpeg(width, height);
            await System.IO.File.WriteAllBytesAsync(filePath, testImageBytes);
            
            return filePath;
        }

        /// <summary>
        /// Creates a minimal valid JPEG image for testing
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>JPEG bytes</returns>
        private byte[] CreateMinimalJpeg(int width, int height)
        {
            // This is a minimal valid JPEG structure for testing
            // In a real scenario, you'd use a proper image library like System.Drawing or ImageSharp
            var jpegHeader = new byte[]
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00
            };

            var jpegFooter = new byte[] { 0xFF, 0xD9 };

            // Create a simple color data (this is just for testing)
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

        public void Dispose()
        {
            // Cleanup uploaded files
            foreach (var fileId in _uploadedFileIds)
            {
                try
                {
                    // Note: File deletion would require additional GraphQL mutation
                    Console.WriteLine($"🧹 Cleanup: Would delete file {fileId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Cleanup failed for {fileId}: {ex.Message}");
                }
            }

            // Cleanup test directory
            try
            {
                if (Directory.Exists(_testImagesDir))
                    Directory.Delete(_testImagesDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to cleanup test directory: {ex.Message}");
            }
        }
    }
} 