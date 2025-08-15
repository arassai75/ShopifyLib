using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Example demonstrating how to work with variant-specific images
    /// </summary>
    public class VariantImageExample
    {
        private readonly ShopifyClient _client;

        public VariantImageExample(ShopifyClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Demonstrates how to create a product with variants and assign specific images to each variant
        /// </summary>
        public async Task CreateProductWithVariantImagesAsync()
        {
            Console.WriteLine("=== Creating Product with Variant-Specific Images ===");

            // Step 1: Create a product with multiple variants
            var product = new Product
            {
                Title = $"T-Shirt Collection {DateTime.UtcNow:yyyyMMddHHmmss}",
                BodyHtml = "<p>A collection of t-shirts in different colors and sizes.</p>",
                Vendor = "Fashion Brand",
                ProductType = "Apparel",
                Status = "active",
                Published = true,
                Variants = new List<ProductVariant>
                {
                    new ProductVariant
                    {
                        Title = "Small / Red",
                        Price = "24.99",
                        Sku = $"TSHIRT-SMALL-RED-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 50
                    },
                    new ProductVariant
                    {
                        Title = "Medium / Red",
                        Price = "24.99",
                        Sku = $"TSHIRT-MEDIUM-RED-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 75
                    },
                    new ProductVariant
                    {
                        Title = "Large / Red",
                        Price = "24.99",
                        Sku = $"TSHIRT-LARGE-RED-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 60
                    },
                    new ProductVariant
                    {
                        Title = "Small / Blue",
                        Price = "24.99",
                        Sku = $"TSHIRT-SMALL-BLUE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 45
                    },
                    new ProductVariant
                    {
                        Title = "Medium / Blue",
                        Price = "24.99",
                        Sku = $"TSHIRT-MEDIUM-BLUE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 80
                    },
                    new ProductVariant
                    {
                        Title = "Large / Blue",
                        Price = "24.99",
                        Sku = $"TSHIRT-LARGE-BLUE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 55
                    }
                }
            };

            var createdProduct = await _client.Products.CreateAsync(product);
            Console.WriteLine($"✅ Created product: {createdProduct.Title} (ID: {createdProduct.Id})");

            try
            {
                // Step 2: Group variants by color
                var redVariants = createdProduct.Variants.Where(v => v.Title.Contains("Red")).ToList();
                var blueVariants = createdProduct.Variants.Where(v => v.Title.Contains("Blue")).ToList();

                Console.WriteLine($"Red variants: {redVariants.Count}");
                Console.WriteLine($"Blue variants: {blueVariants.Count}");

                // Step 3: Upload images for each color group
                var redImage = await _client.Images.UploadImageFromUrlForVariantsAsync(
                    productId: createdProduct.Id,
                    imageUrl: "https://httpbin.org/image/png", // Red t-shirt image
                    variantIds: redVariants.Select(v => v.Id).ToList(),
                    altText: "Red T-Shirt",
                    position: 1
                );

                var blueImage = await _client.Images.UploadImageFromUrlForVariantsAsync(
                    productId: createdProduct.Id,
                    imageUrl: "https://httpbin.org/image/jpeg", // Blue t-shirt image
                    variantIds: blueVariants.Select(v => v.Id).ToList(),
                    altText: "Blue T-Shirt",
                    position: 2
                );

                Console.WriteLine($"✅ Uploaded red image (ID: {redImage.Id}) for {redVariants.Count} variants");
                Console.WriteLine($"✅ Uploaded blue image (ID: {blueImage.Id}) for {blueVariants.Count} variants");

                // Step 4: Verify variant associations
                foreach (var variant in createdProduct.Variants)
                {
                    var variantImages = await _client.Images.GetVariantImagesAsync(createdProduct.Id, variant.Id);
                    var color = variant.Title.Contains("Red") ? "Red" : "Blue";
                    Console.WriteLine($"Variant {variant.Title} has {variantImages.Count} {color} image(s)");
                }

                Console.WriteLine("🎉 Product created successfully with variant-specific images!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                
                // Clean up on error
                try
                {
                    await _client.Products.DeleteAsync(createdProduct.Id);
                    Console.WriteLine($"Cleaned up product {createdProduct.Id}");
                }
                catch
                {
                    Console.WriteLine($"Warning: Could not clean up product {createdProduct.Id}");
                }
            }
        }

        /// <summary>
        /// Demonstrates how to manage variant images for an existing product
        /// </summary>
        public async Task ManageVariantImagesForExistingProductAsync(long productId)
        {
            Console.WriteLine($"=== Managing Variant Images for Product {productId} ===");

            // Step 1: Get the product and its variants
            var product = await _client.Products.GetAsync(productId);
            Console.WriteLine($"Product: {product.Title}");
            Console.WriteLine($"Variants: {product.Variants.Count}");

            if (product.Variants.Count == 0)
            {
                Console.WriteLine("❌ This product has no variants. Cannot demonstrate variant-specific images.");
                return;
            }

            // Step 2: Get current images
            var currentImages = await _client.Images.GetProductImagesAsync(productId);
            Console.WriteLine($"Current images: {currentImages.Count}");

            // Step 3: Show current variant-image associations
            foreach (var variant in product.Variants)
            {
                var variantImages = await _client.Images.GetVariantImagesAsync(productId, variant.Id);
                Console.WriteLine($"Variant {variant.Title} (ID: {variant.Id}) has {variantImages.Count} associated images");
            }

            // Step 4: Add new variant-specific images
            Console.WriteLine("\n--- Adding New Variant Images ---");
            
            for (int i = 0; i < Math.Min(product.Variants.Count, 3); i++)
            {
                var variant = product.Variants[i];
                var imageUrl = i == 0 ? "https://httpbin.org/image/png" :
                              i == 1 ? "https://httpbin.org/image/jpeg" :
                              "https://httpbin.org/image/webp";

                try
                {
                    var newImage = await _client.Images.UploadImageFromUrlForVariantsAsync(
                        productId: productId,
                        imageUrl: imageUrl,
                        variantIds: new List<long> { variant.Id },
                        altText: $"Specific image for {variant.Title}",
                        position: currentImages.Count + i + 1
                    );

                    Console.WriteLine($"✅ Added image {newImage.Id} for variant {variant.Title}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to add image for variant {variant.Title}: {ex.Message}");
                }
            }

            // Step 5: Update existing image associations
            Console.WriteLine("\n--- Updating Image Associations ---");
            
            if (currentImages.Count > 0)
            {
                var firstImage = currentImages.First();
                var firstVariant = product.Variants.First();
                
                try
                {
                    var updatedImage = await _client.Images.UpdateImageVariantAssociationsAsync(
                        productId: productId,
                        imageId: firstImage.Id,
                        variantIds: new List<long> { firstVariant.Id }
                    );

                    Console.WriteLine($"✅ Updated image {firstImage.Id} to be associated with variant {firstVariant.Title}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to update image associations: {ex.Message}");
                }
            }

            // Step 6: Final state
            Console.WriteLine("\n--- Final State ---");
            var finalImages = await _client.Images.GetProductImagesAsync(productId);
            Console.WriteLine($"Total images: {finalImages.Count}");

            foreach (var variant in product.Variants)
            {
                var variantImages = await _client.Images.GetVariantImagesAsync(productId, variant.Id);
                Console.WriteLine($"Variant {variant.Title}: {variantImages.Count} images");
                
                foreach (var img in variantImages)
                {
                    Console.WriteLine($"  - Image {img.Id}: {img.Alt} (Position: {img.Position})");
                }
            }
        }

        /// <summary>
        /// Demonstrates how to upload local images for specific variants
        /// </summary>
        public async Task UploadLocalImagesForVariantsAsync(long productId, string imageDirectory)
        {
            Console.WriteLine($"=== Uploading Local Images for Product {productId} ===");

            if (!Directory.Exists(imageDirectory))
            {
                Console.WriteLine($"❌ Image directory not found: {imageDirectory}");
                return;
            }

            var product = await _client.Products.GetAsync(productId);
            var imageFiles = Directory.GetFiles(imageDirectory, "*.jpg")
                                    .Concat(Directory.GetFiles(imageDirectory, "*.png"))
                                    .Concat(Directory.GetFiles(imageDirectory, "*.jpeg"))
                                    .ToArray();

            Console.WriteLine($"Found {imageFiles.Length} image files in {imageDirectory}");

            for (int i = 0; i < Math.Min(imageFiles.Length, product.Variants.Count); i++)
            {
                var imagePath = imageFiles[i];
                var variant = product.Variants[i];
                var fileName = Path.GetFileName(imagePath);

                try
                {
                    var image = await _client.Images.UploadImageForVariantsAsync(
                        productId: productId,
                        imagePath: imagePath,
                        variantIds: new List<long> { variant.Id },
                        altText: $"Local image for {variant.Title}: {fileName}",
                        position: i + 1
                    );

                    Console.WriteLine($"✅ Uploaded {fileName} for variant {variant.Title} (Image ID: {image.Id})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to upload {fileName} for variant {variant.Title}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Demonstrates how to use streams for variant image uploads
        /// </summary>
        public async Task UploadStreamImagesForVariantsAsync(long productId)
        {
            Console.WriteLine($"=== Uploading Stream Images for Product {productId} ===");

            var product = await _client.Products.GetAsync(productId);
            
            // Create different test images for each variant
            var testImages = new[]
            {
                new { Name = "red-shirt.png", Data = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==") },
                new { Name = "blue-shirt.png", Data = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==") },
                new { Name = "green-shirt.png", Data = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==") }
            };

            for (int i = 0; i < Math.Min(testImages.Length, product.Variants.Count); i++)
            {
                var testImage = testImages[i];
                var variant = product.Variants[i];

                using (var stream = new MemoryStream(testImage.Data))
                {
                    try
                    {
                        var image = await _client.Images.UploadImageForVariantsAsync(
                            productId: productId,
                            imageStream: stream,
                            fileName: testImage.Name,
                            variantIds: new List<long> { variant.Id },
                            altText: $"Stream upload for {variant.Title}",
                            position: i + 1
                        );

                        Console.WriteLine($"✅ Uploaded {testImage.Name} for variant {variant.Title} (Image ID: {image.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to upload {testImage.Name} for variant {variant.Title}: {ex.Message}");
                    }
                }
            }
        }
    }
} 