using System;
using System.Collections.Generic;
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
    /// Integration tests for ImageTransformationService with real Shopify uploads
    /// </summary>
    [IntegrationTest]
    public class ImageTransformationIntegrationTests : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly ImageTransformationService _transformationService;
        private string? _uploadedFileId;

        public ImageTransformationIntegrationTests()
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
            _transformationService = new ImageTransformationService();
        }

        [Fact]
        public async Task UploadAndTransform_CloudinaryImage_DemonstratesAllTransformations()
        {
            // Arrange
            var imageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            var altText = "Cloudinary Sample Image - Transformation Test";

            Console.WriteLine("=== IMAGE TRANSFORMATION INTEGRATION TEST ===");
            Console.WriteLine("This test uploads an image and demonstrates all transformation capabilities");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Step 1: Upload image via GraphQL
                Console.WriteLine("📤 Step 1: Uploading image via GraphQL...");
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
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
                }

                // Step 2: Demonstrate transformations using the CDN URL
                if (uploadedFile.Image?.Src != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("🔄 Step 2: Demonstrating image transformations...");
                    Console.WriteLine($"Base CDN URL: {uploadedFile.Image.Src}");
                    Console.WriteLine();

                    // Demonstrate various transformations
                    DemonstrateTransformations(uploadedFile.Image.Src);
                }
                else
                {
                    Console.WriteLine("⚠️  No CDN URL available for transformations");
                }

                // Step 3: Create responsive URLs
                if (uploadedFile.Image?.Src != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("📱 Step 3: Creating responsive URLs...");
                    var responsiveUrls = _transformationService.CreateResponsiveUrls(uploadedFile.Image.Src);
                    
                    foreach (var kvp in responsiveUrls)
                    {
                        Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("✅ Image transformation integration test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
                throw;
            }
        }

        [Fact]
        public async Task UploadAndTransform_WithCustomTransformations_DemonstratesAdvancedFeatures()
        {
            // Arrange
            var imageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            var altText = "Cloudinary Sample Image - Advanced Transformations";

            Console.WriteLine("=== ADVANCED TRANSFORMATION TEST ===");
            Console.WriteLine("This test demonstrates advanced transformation features");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine();

            try
            {
                // Upload image
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
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

                if (uploadedFile.Image?.Src == null)
                {
                    Console.WriteLine("⚠️  No CDN URL available for transformations");
                    return;
                }

                Console.WriteLine($"✅ Upload successful! File ID: {uploadedFile.Id}");
                Console.WriteLine($"Base CDN URL: {uploadedFile.Image.Src}");
                Console.WriteLine();

                // Demonstrate advanced transformations
                Console.WriteLine("🎨 Advanced Transformations:");
                Console.WriteLine();

                // 1. High-quality WebP for modern browsers
                var webpHighQuality = _transformationService.CreateWebPUrl(uploadedFile.Image.Src, 95);
                Console.WriteLine($"1. High-Quality WebP (95%): {webpHighQuality}");

                // 2. Square thumbnail with top crop
                var squareThumbnail = _transformationService.CreateThumbnailUrl(uploadedFile.Image.Src, 200, CropMode.Top);
                Console.WriteLine($"2. Square Thumbnail (200x200, top crop): {squareThumbnail}");

                // 3. Wide banner format
                var wideBanner = _transformationService.BuildTransformedUrl(uploadedFile.Image.Src, new ImageTransformations
                {
                    Width = 1200,
                    Height = 400,
                    Crop = CropMode.Center,
                    Format = ImageFormat.Jpg,
                    Quality = 90
                });
                Console.WriteLine($"3. Wide Banner (1200x400): {wideBanner}");

                // 4. Mobile-optimized
                var mobileOptimized = _transformationService.BuildTransformedUrl(uploadedFile.Image.Src, new ImageTransformations
                {
                    Width = 600,
                    Height = 800,
                    Crop = CropMode.Center,
                    Format = ImageFormat.WebP,
                    Quality = 80
                });
                Console.WriteLine($"4. Mobile Optimized (600x800, WebP): {mobileOptimized}");

                // 5. Print quality
                var printQuality = _transformationService.BuildTransformedUrl(uploadedFile.Image.Src, new ImageTransformations
                {
                    Width = 2400,
                    Height = 1800,
                    Format = ImageFormat.Png,
                    Quality = 100
                });
                Console.WriteLine($"5. Print Quality (2400x1800, PNG): {printQuality}");

                Console.WriteLine();
                Console.WriteLine("✅ Advanced transformation test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
                throw;
            }
        }

        private void DemonstrateTransformations(string baseCdnUrl)
        {
            Console.WriteLine("🖼️  Basic Transformations:");
            
            // Thumbnail
            var thumbnail = _transformationService.CreateThumbnailUrl(baseCdnUrl);
            Console.WriteLine($"   Thumbnail (150x150): {thumbnail}");

            // Medium
            var medium = _transformationService.CreateMediumUrl(baseCdnUrl);
            Console.WriteLine($"   Medium (800x600): {medium}");

            // Large
            var large = _transformationService.CreateLargeUrl(baseCdnUrl);
            Console.WriteLine($"   Large (1200x800): {large}");

            // WebP
            var webp = _transformationService.CreateWebPUrl(baseCdnUrl);
            Console.WriteLine($"   WebP (85% quality): {webp}");

            // High Quality
            var highQuality = _transformationService.CreateHighQualityUrl(baseCdnUrl);
            Console.WriteLine($"   High Quality (100%): {highQuality}");

            Console.WriteLine();
            Console.WriteLine("🎯 Custom Transformations:");

            // Custom transformations
            var custom1 = _transformationService.BuildTransformedUrl(baseCdnUrl, new ImageTransformations
            {
                Width = 500,
                Height = 300,
                Crop = CropMode.Top,
                Quality = 90
            });
            Console.WriteLine($"   Custom 1 (500x300, top crop, 90%): {custom1}");

            var custom2 = _transformationService.BuildTransformedUrl(baseCdnUrl, new ImageTransformations
            {
                Width = 1000,
                Height = 1000,
                Crop = CropMode.Center,
                Format = ImageFormat.Png,
                Quality = 95
            });
            Console.WriteLine($"   Custom 2 (1000x1000, PNG, 95%): {custom2}");

            var custom3 = _transformationService.BuildTransformedUrl(baseCdnUrl, new ImageTransformations
            {
                Scale = 2.0,
                Format = ImageFormat.WebP,
                Quality = 80
            });
            Console.WriteLine($"   Custom 3 (2x scale, WebP, 80%): {custom3}");
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