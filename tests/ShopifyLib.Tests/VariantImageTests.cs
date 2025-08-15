using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Xunit;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Tests for variant-specific image operations
    /// </summary>
    public class VariantImageTests : IDisposable
    {
        private readonly ShopifyClient _client;

        public VariantImageTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var config = new ShopifyConfig
            {
                ShopDomain = configuration["Shopify:ShopDomain"] ?? throw new InvalidOperationException("Shopify:ShopDomain not configured"),
                AccessToken = configuration["Shopify:AccessToken"] ?? throw new InvalidOperationException("Shopify:AccessToken not configured")
            };

            _client = new ShopifyClient(config);
        }

        [Fact]
        public async Task UploadImageForVariants_FromUrl_ShouldAssociateWithVariants()
        {
            // Get an existing product with variants
            var products = await _client.Products.GetAllAsync(limit: 10);
            var productWithVariants = products.FirstOrDefault(p => p.Variants.Count > 1);
            
            if (productWithVariants == null)
            {
                // Skip test if no product with variants exists
                Console.WriteLine("No product with variants found, skipping test");
                return;
            }

            var variantIds = productWithVariants.Variants.Take(2).Select(v => v.Id).ToList();
            var originalImageCount = (await _client.Images.GetProductImagesAsync(productWithVariants.Id)).Count;

            // Upload image for specific variants using the LEGO URL (same as successful product root test)
            var image = await _client.Images.UploadImageFromUrlForVariantsAsync(
                productId: productWithVariants.Id,
                imageUrl: "https://www.lego.com/cdn/cs/set/assets/blt742e8599eb5e8931/40649.png",
                variantIds: variantIds,
                altText: "Variant-specific LEGO image test",
                position: originalImageCount + 1
            );

            // Verify the image was uploaded
            Assert.NotNull(image);
            Assert.True(image.Id > 0);
            Assert.Contains("cdn.shopify.com", image.Src);
            Assert.Equal("Variant-specific LEGO image test", image.Alt);

            // Verify variant associations
            Assert.NotNull(image.VariantIds);
            Assert.Equal(variantIds.Count, image.VariantIds.Count);
            foreach (var variantId in variantIds)
            {
                Assert.Contains(variantId, image.VariantIds);
            }

            // Verify we can retrieve variant-specific images
            var variantImages = await _client.Images.GetVariantImagesAsync(productWithVariants.Id, variantIds[0]);
            Assert.Contains(variantImages, img => img.Id == image.Id);

            Console.WriteLine($"✅ Successfully uploaded variant-specific image:");
            Console.WriteLine($"   Image ID: {image.Id}");
            Console.WriteLine($"   Associated with {variantIds.Count} variants: {string.Join(", ", variantIds)}");
            Console.WriteLine($"   Variant images count for variant {variantIds[0]}: {variantImages.Count}");
        }

        [Fact]
        public async Task GetVariantImages_ShouldReturnOnlyAssociatedImages()
        {
            // Get an existing product with variants
            var products = await _client.Products.GetAllAsync(limit: 10);
            var productWithVariants = products.FirstOrDefault(p => p.Variants.Count > 1);
            
            if (productWithVariants == null)
            {
                Console.WriteLine("No product with variants found, skipping test");
                return;
            }

            var variant = productWithVariants.Variants.First();
            var allImages = await _client.Images.GetProductImagesAsync(productWithVariants.Id);
            var variantImages = await _client.Images.GetVariantImagesAsync(productWithVariants.Id, variant.Id);

            // Verify that variant images are a subset of all images
            Assert.True(variantImages.Count <= allImages.Count);
            
            // Verify that all variant images are actually associated with the variant
            foreach (var image in variantImages)
            {
                Assert.Contains(variant.Id, image.VariantIds);
            }

            Console.WriteLine($"✅ Variant image filtering works correctly:");
            Console.WriteLine($"   Product {productWithVariants.Id} has {allImages.Count} total images");
            Console.WriteLine($"   Variant {variant.Id} has {variantImages.Count} associated images");
        }

        [Fact]
        public async Task UpdateImageVariantAssociations_ShouldModifyAssociations()
        {
            // Get an existing product with variants
            var products = await _client.Products.GetAllAsync(limit: 10);
            var productWithVariants = products.FirstOrDefault(p => p.Variants.Count > 2);
            
            if (productWithVariants == null)
            {
                Console.WriteLine("No product with enough variants found, skipping test");
                return;
            }

            var allVariants = productWithVariants.Variants.ToList();
            var originalVariantIds = new List<long> { allVariants[0].Id, allVariants[1].Id };
            var newVariantIds = new List<long> { allVariants[1].Id, allVariants[2].Id };

            // First, upload an image with original variant associations using LEGO URL
            var image = await _client.Images.UploadImageFromUrlForVariantsAsync(
                productId: productWithVariants.Id,
                imageUrl: "https://www.lego.com/cdn/cs/set/assets/blt742e8599eb5e8931/40649.png",
                variantIds: originalVariantIds,
                altText: "LEGO image for variant association update test"
            );

            // Verify original associations
            Assert.Equal(originalVariantIds.Count, image.VariantIds.Count);
            foreach (var variantId in originalVariantIds)
            {
                Assert.Contains(variantId, image.VariantIds);
            }

            // Update variant associations
            var updatedImage = await _client.Images.UpdateImageVariantAssociationsAsync(
                productId: productWithVariants.Id,
                imageId: image.Id,
                variantIds: newVariantIds
            );

            // Verify new associations
            Assert.Equal(newVariantIds.Count, updatedImage.VariantIds.Count);
            foreach (var variantId in newVariantIds)
            {
                Assert.Contains(variantId, updatedImage.VariantIds);
            }

            // Verify that old associations are removed
            Assert.DoesNotContain(originalVariantIds[0], updatedImage.VariantIds);

            Console.WriteLine($"✅ Successfully updated variant associations:");
            Console.WriteLine($"   Image ID: {image.Id}");
            Console.WriteLine($"   Original variants: {string.Join(", ", originalVariantIds)}");
            Console.WriteLine($"   New variants: {string.Join(", ", newVariantIds)}");
        }

        [Fact]
        public async Task UploadImageForVariants_FromStream_ShouldWork()
        {
            // Create a simple test image in memory
            var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="); // 1x1 transparent PNG
            
            // Get an existing product with variants
            var products = await _client.Products.GetAllAsync(limit: 10);
            var productWithVariants = products.FirstOrDefault(p => p.Variants.Count > 0);
            
            if (productWithVariants == null)
            {
                Console.WriteLine("No product with variants found, skipping test");
                return;
            }

            var variantId = productWithVariants.Variants.First().Id;
            var variantIds = new List<long> { variantId };

            using (var stream = new MemoryStream(imageBytes))
            {
                var image = await _client.Images.UploadImageForVariantsAsync(
                    productId: productWithVariants.Id,
                    imageStream: stream,
                    fileName: "test-variant-image.png",
                    variantIds: variantIds,
                    altText: "Stream upload test for variants"
                );

                // Verify the image was uploaded
                Assert.NotNull(image);
                Assert.True(image.Id > 0);
                Assert.Contains("cdn.shopify.com", image.Src);
                Assert.Equal("Stream upload test for variants", image.Alt);
                Assert.Contains(variantId, image.VariantIds);

                Console.WriteLine($"✅ Successfully uploaded variant image from stream:");
                Console.WriteLine($"   Image ID: {image.Id}");
                Console.WriteLine($"   Associated with variant: {variantId}");
            }
        }

        [Fact]
        public async Task CreateProductWithVariantsAndImages_ShouldWork()
        {
            // Create a test product with variants and options
            var testProduct = new Product
            {
                Title = $"Variant Image Test Product {DateTime.UtcNow:yyyyMMddHHmmss}",
                BodyHtml = "<p>This product was created to test variant-specific image functionality.</p>",
                Vendor = "Shopify Library Test",
                ProductType = "Test Product",
                Status = "active",
                Published = false, // Start as draft to avoid validation issues
                Options = new List<ProductOption>
                {
                    new ProductOption { Name = "Size", Values = new List<string> { "Small", "Medium", "Large" } }
                },
                Variants = new List<ProductVariant>
                {
                    new ProductVariant
                    {
                        Title = "Small",
                        Option1 = "Small",
                        Price = "19.99",
                        Sku = $"TEST-SMALL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 10,
                        InventoryManagement = "shopify",
                        InventoryPolicy = "continue"
                    },
                    new ProductVariant
                    {
                        Title = "Medium",
                        Option1 = "Medium",
                        Price = "24.99",
                        Sku = $"TEST-MEDIUM-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 15,
                        InventoryManagement = "shopify",
                        InventoryPolicy = "continue"
                    },
                    new ProductVariant
                    {
                        Title = "Large",
                        Option1 = "Large",
                        Price = "29.99",
                        Sku = $"TEST-LARGE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 20,
                        InventoryManagement = "shopify",
                        InventoryPolicy = "continue"
                    }
                }
            };

            // Create the product
            var createdProduct = await _client.Products.CreateAsync(testProduct);
            Console.WriteLine($"Created test product with ID: {createdProduct.Id}");

            try
            {
                // Upload different images for each variant
                var variantImages = new List<ProductImage>();

                for (int i = 0; i < createdProduct.Variants.Count; i++)
                {
                    var variant = createdProduct.Variants[i];
                    // Use LEGO URL for each variant (same as successful product root test)
                    var image = await _client.Images.UploadImageFromUrlForVariantsAsync(
                        productId: createdProduct.Id,
                        imageUrl: "https://www.lego.com/cdn/cs/set/assets/blt742e8599eb5e8931/40649.png",
                        variantIds: new List<long> { variant.Id },
                        altText: $"LEGO image for {variant.Title} variant",
                        position: i + 1
                    );

                    variantImages.Add(image);
                    Console.WriteLine($"Uploaded LEGO image for {variant.Title} variant: {image.Id}");
                }

                // Verify each variant has its own image
                foreach (var variant in createdProduct.Variants)
                {
                    var variantImagesForVariant = await _client.Images.GetVariantImagesAsync(createdProduct.Id, variant.Id);
                    Assert.NotEmpty(variantImagesForVariant);
                    Console.WriteLine($"Variant {variant.Title} has {variantImagesForVariant.Count} images");
                }

                Console.WriteLine("✅ Successfully created product with variant-specific images");
            }
            finally
            {
                // NOTE: Product is NOT deleted so you can see it in the dashboard
                Console.WriteLine($"✅ Product created and available in dashboard:");
                Console.WriteLine($"   Product ID: {createdProduct.Id}");
                Console.WriteLine($"   Product Title: {createdProduct.Title}");
                Console.WriteLine($"   Each variant has a LEGO image associated with it");
            }
        }

        [Fact]
        public async Task CreateProductWithVariantsAndLegoImage_ShouldWork()
        {
            // Arrange: Create a test product with variants and options
            var testProduct = new Product
            {
                Title = $"LEGO Variant Image Test {DateTime.UtcNow:yyyyMMddHHmmss}",
                BodyHtml = "<p>This product was created to test variant-specific image functionality with a LEGO image.</p>",
                Vendor = "Shopify Library Test",
                ProductType = "Test Product",
                Status = "active",
                Published = false, // Start as draft to avoid validation issues
                Options = new List<ProductOption>
                {
                    new ProductOption { Name = "Size", Values = new List<string> { "Small", "Medium", "Large" } }
                },
                Variants = new List<ProductVariant>
                {
                    new ProductVariant
                    {
                        Title = "LEGO Small",
                        Option1 = "Small",
                        Price = "9.99",
                        Sku = $"LEGO-SMALL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 10,
                        InventoryManagement = "shopify",
                        InventoryPolicy = "continue"
                    },
                    new ProductVariant
                    {
                        Title = "LEGO Medium",
                        Option1 = "Medium",
                        Price = "14.99",
                        Sku = $"LEGO-MEDIUM-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 15,
                        InventoryManagement = "shopify",
                        InventoryPolicy = "continue"
                    },
                    new ProductVariant
                    {
                        Title = "LEGO Large",
                        Option1 = "Large",
                        Price = "19.99",
                        Sku = $"LEGO-LARGE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        InventoryQuantity = 20,
                        InventoryManagement = "shopify",
                        InventoryPolicy = "continue"
                    }
                }
            };

            // Act: Create the product
            var createdProduct = await _client.Products.CreateAsync(testProduct);
            Console.WriteLine($"Created test product with ID: {createdProduct.Id}");

            string legoImageUrl = "https://www.lego.com/cdn/cs/set/assets/blt742e8599eb5e8931/40649.png";
            var uploadedImages = new List<ProductImage>();

            try
            {
                // Attach the LEGO image to each variant
                for (int i = 0; i < createdProduct.Variants.Count; i++)
                {
                    var variant = createdProduct.Variants[i];
                    var image = await _client.Images.UploadImageFromUrlForVariantsAsync(
                        productId: createdProduct.Id,
                        imageUrl: legoImageUrl,
                        variantIds: new List<long> { variant.Id },
                        altText: $"LEGO image for {variant.Title}",
                        position: i + 1
                    );
                    uploadedImages.Add(image);
                    Console.WriteLine($"Uploaded LEGO image for {variant.Title}: {image.Id}");
                }

                // Assert: Each variant has at least one image
                foreach (var variant in createdProduct.Variants)
                {
                    var variantImages = await _client.Images.GetVariantImagesAsync(createdProduct.Id, variant.Id);
                    Assert.NotEmpty(variantImages);
                    // Check that the image has the correct alt text instead of checking the URL
                    Assert.Contains(variantImages, img => img.Alt.Contains("LEGO image for"));
                    Console.WriteLine($"Variant {variant.Title} has {variantImages.Count} images");
                }

                Console.WriteLine("✅ Successfully created product with LEGO image for each variant");
            }
            finally
            {
                // NOTE: Product is NOT deleted so you can see it in the dashboard
                Console.WriteLine($"✅ Product created and available in dashboard:");
                Console.WriteLine($"   Product ID: {createdProduct.Id}");
                Console.WriteLine($"   Product Title: {createdProduct.Title}");
                Console.WriteLine($"   Each variant has a LEGO image associated with it");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 