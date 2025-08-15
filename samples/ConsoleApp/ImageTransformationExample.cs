using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifyLib;
using ShopifyLib.Models;
using ShopifyLib.Services;
using ShopifyLib.Configuration;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp
{
    /// <summary>
    /// Example demonstrating image transformation capabilities
    /// </summary>
    public class ImageTransformationExample
    {
        private readonly ShopifyClient _client;
        private readonly ImageTransformationService _transformationService;

        public ImageTransformationExample()
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
            _transformationService = new ImageTransformationService();
        }

        public async Task RunExample()
        {
            Console.WriteLine("=== SHOPIFY IMAGE TRANSFORMATION EXAMPLE ===");
            Console.WriteLine("This example demonstrates how to upload an image and create various transformations");
            Console.WriteLine();

            try
            {
                // Step 1: Upload an image
                var imageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
                var altText = "Sample Image for Transformation Demo";

                Console.WriteLine($"📤 Step 1: Uploading image from {imageUrl}");
                
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
                Console.WriteLine($"✅ Upload successful!");
                Console.WriteLine($"   File ID: {uploadedFile.Id}");
                Console.WriteLine($"   File Status: {uploadedFile.FileStatus}");
                
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"   Original Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"   Original Src: {uploadedFile.Image.Src}");
                }

                // Step 2: Demonstrate transformations
                if (uploadedFile.Image?.Src != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("🔄 Step 2: Creating image transformations...");
                    Console.WriteLine($"Base CDN URL: {uploadedFile.Image.Src}");
                    Console.WriteLine();

                    // Basic transformations
                    Console.WriteLine("📱 Basic Transformations:");
                    Console.WriteLine($"   Thumbnail: {_transformationService.CreateThumbnailUrl(uploadedFile.Image.Src)}");
                    Console.WriteLine($"   Medium: {_transformationService.CreateMediumUrl(uploadedFile.Image.Src)}");
                    Console.WriteLine($"   Large: {_transformationService.CreateLargeUrl(uploadedFile.Image.Src)}");
                    Console.WriteLine($"   WebP: {_transformationService.CreateWebPUrl(uploadedFile.Image.Src)}");
                    Console.WriteLine($"   High Quality: {_transformationService.CreateHighQualityUrl(uploadedFile.Image.Src)}");

                    // Custom transformations
                    Console.WriteLine();
                    Console.WriteLine("🎨 Custom Transformations:");
                    
                    var squareThumbnail = _transformationService.CreateThumbnailUrl(uploadedFile.Image.Src, 200, CropMode.Top);
                    Console.WriteLine($"   Square Thumbnail (200x200, top crop): {squareThumbnail}");

                    var wideBanner = _transformationService.BuildTransformedUrl(uploadedFile.Image.Src, new ImageTransformations
                    {
                        Width = 1200,
                        Height = 400,
                        Crop = CropMode.Center,
                        Format = ImageFormat.Jpg,
                        Quality = 90
                    });
                    Console.WriteLine($"   Wide Banner (1200x400): {wideBanner}");

                    var mobileOptimized = _transformationService.BuildTransformedUrl(uploadedFile.Image.Src, new ImageTransformations
                    {
                        Width = 600,
                        Height = 800,
                        Crop = CropMode.Center,
                        Format = ImageFormat.WebP,
                        Quality = 80
                    });
                    Console.WriteLine($"   Mobile Optimized (600x800, WebP): {mobileOptimized}");

                    // Responsive URLs
                    Console.WriteLine();
                    Console.WriteLine("📱 Responsive URLs for different screen sizes:");
                    var responsiveUrls = _transformationService.CreateResponsiveUrls(uploadedFile.Image.Src);
                    
                    foreach (var kvp in responsiveUrls)
                    {
                        Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
                    }

                    // Real-world usage examples
                    Console.WriteLine();
                    Console.WriteLine("🌐 Real-World Usage Examples:");
                    Console.WriteLine();

                    // Example 1: Product thumbnail
                    Console.WriteLine("1. Product Thumbnail (150x150, center crop):");
                    var productThumbnail = _transformationService.CreateThumbnailUrl(uploadedFile.Image.Src, 150, CropMode.Center);
                    Console.WriteLine($"   <img src=\"{productThumbnail}\" alt=\"{altText}\" />");
                    Console.WriteLine();

                    // Example 2: Product gallery (medium size)
                    Console.WriteLine("2. Product Gallery (800x600, center crop):");
                    var productGallery = _transformationService.CreateMediumUrl(uploadedFile.Image.Src, 800, 600, CropMode.Center);
                    Console.WriteLine($"   <img src=\"{productGallery}\" alt=\"{altText}\" />");
                    Console.WriteLine();

                    // Example 3: Hero banner (large size)
                    Console.WriteLine("3. Hero Banner (1200x800, center crop):");
                    var heroBanner = _transformationService.CreateLargeUrl(uploadedFile.Image.Src, 1200, 800, CropMode.Center);
                    Console.WriteLine($"   <img src=\"{heroBanner}\" alt=\"{altText}\" />");
                    Console.WriteLine();

                    // Example 4: WebP for modern browsers
                    Console.WriteLine("4. WebP for Modern Browsers (85% quality):");
                    var webpVersion = _transformationService.CreateWebPUrl(uploadedFile.Image.Src, 85);
                    Console.WriteLine($"   <picture>");
                    Console.WriteLine($"     <source srcset=\"{webpVersion}\" type=\"image/webp\">");
                    Console.WriteLine($"     <img src=\"{uploadedFile.Image.Src}\" alt=\"{altText}\" />");
                    Console.WriteLine($"   </picture>");
                    Console.WriteLine();

                    // Example 5: Responsive image with srcset
                    Console.WriteLine("5. Responsive Image with srcset:");
                    var thumbnail = responsiveUrls["thumbnail"];
                    var small = responsiveUrls["small"];
                    var medium = responsiveUrls["medium"];
                    var large = responsiveUrls["large"];
                    
                    Console.WriteLine($"   <img src=\"{medium}\"");
                    Console.WriteLine($"        srcset=\"{thumbnail} 150w, {small} 300w, {medium} 800w, {large} 1200w\"");
                    Console.WriteLine($"        sizes=\"(max-width: 600px) 150px, (max-width: 900px) 300px, (max-width: 1200px) 800px, 1200px\"");
                    Console.WriteLine($"        alt=\"{altText}\" />");
                }
                else
                {
                    Console.WriteLine("⚠️  No CDN URL available for transformations");
                }

                Console.WriteLine();
                Console.WriteLine("✅ Image transformation example completed successfully!");
                Console.WriteLine();
                Console.WriteLine("💡 Tips:");
                Console.WriteLine("   • Use WebP format for better performance on modern browsers");
                Console.WriteLine("   • Create multiple sizes for responsive design");
                Console.WriteLine("   • Use appropriate crop modes for different use cases");
                Console.WriteLine("   • Consider quality settings based on your needs");
                Console.WriteLine("   • Always provide alt text for accessibility");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Example failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex}");
            }
        }
    }
} 