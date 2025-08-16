using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ShopifyLib;
using ShopifyLib.Models;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Shopify Library Demo");
            Console.WriteLine("===================");

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Bind configuration to ShopifyConfig
            var shopifyConfig = new ShopifyConfig();
            configuration.GetSection("Shopify").Bind(shopifyConfig);

            // Validate configuration
            if (!shopifyConfig.IsValid())
            {
                Console.WriteLine("❌ Invalid Shopify configuration!");
                Console.WriteLine("Please update appsettings.json with your Shopify credentials:");
                Console.WriteLine("- ShopDomain: your-shop.myshopify.com");
                Console.WriteLine("- AccessToken: your-access-token");
                return;
            }

            Console.WriteLine($"✅ Connected to: {shopifyConfig.ShopDomain}");
            Console.WriteLine($"📊 API Version: {shopifyConfig.ApiVersion}");
            Console.WriteLine();

            try
            {
                // Create Shopify client
                using var client = new ShopifyClient(shopifyConfig);

                // Demo: Get product count
                await DemoGetProductCount(client);

                // Demo: Get products
                await DemoGetProducts(client);

                // Demo: Create a test product
                await DemoCreateProduct(client);

                // Demo: Get metafields
                await DemoGetMetafields(client);

                // Demo: Upload image using GraphQL
                await DemoUploadImage(client);

                // Demo: Image transformations
                await DemoImageTransformations(client);

                // Demo: Staged upload (new approach for problematic URLs)
                await DemoStagedUpload(client);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task DemoGetProductCount(ShopifyClient client)
        {
            Console.WriteLine("📊 Getting product count...");
            try
            {
                var count = await client.Products.GetCountAsync();
                Console.WriteLine($"✅ Total products: {count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get product count: {ex.Message}");
            }
            Console.WriteLine();
        }

        static async Task DemoGetProducts(ShopifyClient client)
        {
            Console.WriteLine("🛍️ Getting products...");
            try
            {
                var products = await client.Products.GetAllAsync(limit: 5);
                Console.WriteLine($"✅ Found {products.Count} products:");
                
                foreach (var product in products)
                {
                    Console.WriteLine($"   - {product.Title} (ID: {product.Id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get products: {ex.Message}");
            }
            Console.WriteLine();
        }

        static async Task DemoCreateProduct(ShopifyClient client)
        {
            Console.WriteLine("➕ Creating test product...");
            try
            {
                var product = new Product
                {
                    Title = "Test Product from .NET Library",
                    BodyHtml = "<p>This is a test product created using the Shopify .NET library.</p>",
                    Vendor = "Test Vendor",
                    ProductType = "Test Type",
                    Status = "draft",
                    Published = false
                };

                var createdProduct = await client.Products.CreateAsync(product);
                Console.WriteLine($"✅ Created product: {createdProduct.Title} (ID: {createdProduct.Id})");

                // Clean up - delete the test product
                Console.WriteLine("🗑️ Cleaning up test product...");
                await client.Products.DeleteAsync(createdProduct.Id);
                Console.WriteLine("✅ Test product deleted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create product: {ex.Message}");
            }
            Console.WriteLine();
        }

        static async Task DemoGetMetafields(ShopifyClient client)
        {
            Console.WriteLine("🏷️ Getting metafield definitions...");
            try
            {
                var definitions = await client.Metafields.GetDefinitionsAsync();
                Console.WriteLine($"✅ Found {definitions.Count} metafield definitions:");
                
                foreach (var definition in definitions.Take(3))
                {
                    Console.WriteLine($"   - {definition.Namespace}.{definition.Key} ({definition.Type})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get metafield definitions: {ex.Message}");
            }
            Console.WriteLine();
        }

        static async Task DemoUploadImage(ShopifyClient client)
        {
            Console.WriteLine("📸 Uploading image using GraphQL...");
            try
            {
                // Use the specified  image URL
                var imageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
                var altText = " Gift Image - Console Demo";

                Console.WriteLine($"📸 Image URL: {imageUrl}");
                Console.WriteLine($"📝 Alt Text: {altText}");
                Console.WriteLine();

                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var response = await client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });

                Console.WriteLine("✅ Image upload completed successfully!");
                Console.WriteLine();

                var uploadedFile = response.Files[0];
                
                Console.WriteLine("=== UPLOADED FILE DETAILS ===");
                Console.WriteLine($"📁 File ID: {uploadedFile.Id}");
                Console.WriteLine($"📊 File Status: {uploadedFile.FileStatus}");
                Console.WriteLine($"📝 Alt Text: {uploadedFile.Alt ?? "Not set"}");
                Console.WriteLine($"📅 Created At: {uploadedFile.CreatedAt}");
                
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

                Console.WriteLine();
                Console.WriteLine("=== COMPLETE JSON RESPONSE ===");
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented));
                Console.WriteLine();

                if (uploadedFile.Image != null)
                {
                    Console.WriteLine($"✅ Shopify CDN URL: {uploadedFile.Image.Url ?? "Not available"}");
                    Console.WriteLine($"✅ Primary Source: {uploadedFile.Image.Src ?? "Not available"}");
                }
                            Console.WriteLine("✅ Image uploaded without attaching to any product or variant");
            Console.WriteLine("✅ All response details displayed above");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to upload image: {ex.Message}");
        }
        Console.WriteLine();
    }

    static async Task DemoImageTransformations(ShopifyClient client)
    {
        Console.WriteLine("🎨 Demonstrating image transformations...");
        try
        {
            var transformationExample = new ImageTransformationExample();
            await transformationExample.RunExample();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to demonstrate image transformations: {ex.Message}");
        }
        Console.WriteLine();
    }

    static async Task DemoStagedUpload(ShopifyClient client)
    {
        Console.WriteLine("📤 Demonstrating staged upload functionality...");
        try
        {
            var stagedUploadExample = new StagedUploadExample(client);
            await stagedUploadExample.RunStagedUploadExample();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to demonstrate staged upload: {ex.Message}");
        }
        Console.WriteLine();
    }
}
}
