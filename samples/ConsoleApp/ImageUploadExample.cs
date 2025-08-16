using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Newtonsoft.Json;

namespace ConsoleApp
{
    public class ImageUploadExample
    {
        private readonly ShopifyClient _client;

        public ImageUploadExample()
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

        public async Task RunImageUploadExample()
        {
            Console.WriteLine("=== Shopify Image Upload Example ===");
            Console.WriteLine("This example uploads an image to Shopify using GraphQL");
            Console.WriteLine("and displays all response details including dimensions.");
            Console.WriteLine();

            // Use the specified  image URL
            var imageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = " Gift Image - Console Example";

            Console.WriteLine($"📸 Image URL: {imageUrl}");
            Console.WriteLine($"📝 Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Upload image using GraphQL fileCreate mutation
                Console.WriteLine("🔄 Uploading image to Shopify using GraphQL...");
                
                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                // Display results
                Console.WriteLine("✅ Image upload completed successfully!");
                Console.WriteLine();

                // Display the complete JSON response
                Console.WriteLine("=== COMPLETE SHOPIFY RESPONSE (JSON) ===");
                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
                Console.WriteLine();

                // Display formatted details
                var uploadedFile = response.Files[0];
                
                Console.WriteLine("=== UPLOADED FILE DETAILS ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");
                
                // Display image dimensions if available
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== IMAGE DIMENSIONS ===");
                    Console.WriteLine($"📏 Width: {uploadedFile.Image.Width} pixels");
                    Console.WriteLine($"📐 Height: {uploadedFile.Image.Height} pixels");
                    Console.WriteLine($"📊 Aspect Ratio: {(double)uploadedFile.Image.Width / uploadedFile.Image.Height:F2}");

                    Console.WriteLine();
                    Console.WriteLine("=== SHOPIFY CDN URLS ===");
                    Console.WriteLine($"🌐 Shopify CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"🔗 Original Source: {uploadedFile.Image.OriginalSrc ?? "Not available"}");
                    Console.WriteLine($"🔄 Transformed Source: {uploadedFile.Image.TransformedSrc ?? "Not available"}");
                    Console.WriteLine($"📷 Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                }
                else
                {
                    Console.WriteLine("⚠️  Image dimensions not available in response");
                }

                // Display file status
                Console.WriteLine();
                Console.WriteLine("=== FILE STATUS ===");
                Console.WriteLine($"🔄 Processing Status: {uploadedFile.FileStatus}");
                
                if (uploadedFile.FileStatus.Equals("READY", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✅ File is ready for use!");
                }
                else if (uploadedFile.FileStatus.Equals("UPLOADED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("⏳ File uploaded successfully, processing in progress...");
                }

                // Display GraphQL ID details
                Console.WriteLine();
                Console.WriteLine("=== GRAPHQL ID DETAILS ===");
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

                // Display summary
                Console.WriteLine();
                Console.WriteLine("=== SUMMARY ===");
                Console.WriteLine($"✅ Successfully uploaded image to Shopify");
                Console.WriteLine($"✅ File ID: {uploadedFile.Id}");
                Console.WriteLine($"✅ Status: {uploadedFile.FileStatus}");
                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"✅ Dimensions: {uploadedFile.Image.Width}x{uploadedFile.Image.Height}");
                    Console.WriteLine($"✅ Shopify CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"✅ Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                }
                Console.WriteLine("✅ Image uploaded without attaching to any product or variant");
                Console.WriteLine("✅ All response details displayed above");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _client?.Dispose();
            }
        }
    }
} 