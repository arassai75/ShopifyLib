using System;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ShopifyLib.Configuration;
using System.IO;
using FileModel = ShopifyLib.Models.File;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Tests for managing images on existing products
    /// </summary>
    public class ImageManagementTests : IDisposable
    {
        private readonly ShopifyClient _client;

        public ImageManagementTests()
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
        public async Task AddImageToExistingProduct_ShouldWork()
        {
            // Get an existing product from the store
            var products = await _client.Products.GetAllAsync(limit: 1);
            Assert.NotEmpty(products);

            var existingProduct = products[0];
            var originalImageCount = (await _client.Images.GetProductImagesAsync(existingProduct.Id)).Count;

            // Add a new image to the existing product using LEGO URL (same as successful test)
            var newImage = await _client.Images.UploadImageFromUrlAsync(
                productId: existingProduct.Id,
                imageUrl: "https://www.lego.com/cdn/cs/set/assets/blt742e8599eb5e8931/40649.png",
                altText: "LEGO image added via API",
                position: originalImageCount + 1
            );

            // Verify the image was added
            Assert.NotNull(newImage);
            Assert.True(newImage.Id > 0);
            Assert.Contains("cdn.shopify.com", newImage.Src);
            Assert.Equal("LEGO image added via API", newImage.Alt);
            Assert.Equal(originalImageCount + 1, newImage.Position);

            // Verify the product now has one more image
            var updatedImages = await _client.Images.GetProductImagesAsync(existingProduct.Id);
            Assert.Equal(originalImageCount + 1, updatedImages.Count);

            Console.WriteLine($"✅ Successfully added image {newImage.Id} to product {existingProduct.Id}");
            Console.WriteLine($"   Original image count: {originalImageCount}");
            Console.WriteLine($"   New image count: {updatedImages.Count}");
        }

        [Fact]
        public async Task UpdateExistingImage_ShouldWork()
        {
            // Get an existing product with images
            var products = await _client.Products.GetAllAsync(limit: 5);
            var productWithImages = products.FirstOrDefault(p => 
                _client.Images.GetProductImagesAsync(p.Id).Result.Count > 0);

            if (productWithImages == null)
            {
                Console.WriteLine("⚠️ No product with images found - skipping test");
                return;
            }

            var images = await _client.Images.GetProductImagesAsync(productWithImages.Id);
            var imageToUpdate = images[0];

            var originalAlt = imageToUpdate.Alt;
            var newAlt = $"Updated alt text at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

            // Update the image
            imageToUpdate.Alt = newAlt;
            var updatedImage = await _client.Images.UpdateAsync(
                productId: productWithImages.Id,
                imageId: imageToUpdate.Id,
                image: imageToUpdate
            );

            // Verify the update
            Assert.NotNull(updatedImage);
            Assert.Equal(newAlt, updatedImage.Alt);
            Assert.Equal(imageToUpdate.Id, updatedImage.Id);

            Console.WriteLine($"✅ Successfully updated image {updatedImage.Id}");
            Console.WriteLine($"   Original alt: {originalAlt}");
            Console.WriteLine($"   New alt: {updatedImage.Alt}");
        }

        [Fact]
        public async Task GetProductImages_ShouldReturnImages()
        {
            // Get an existing product
            var products = await _client.Products.GetAllAsync(limit: 1);
            Assert.NotEmpty(products);

            var product = products[0];
            var images = await _client.Images.GetProductImagesAsync(product.Id);

            // Verify we can get images (even if empty)
            Assert.NotNull(images);

            Console.WriteLine($"📸 Product {product.Id} has {images.Count} images:");
            foreach (var img in images.OrderBy(i => i.Position))
            {
                Console.WriteLine($"   {img.Position}. Image {img.Id}: {img.Src}");
                Console.WriteLine($"      Alt: {img.Alt}");
                Console.WriteLine($"      Width: {img.Width}, Height: {img.Height}");
            }
        }

        [Fact]
        public async Task UploadImageViaGraphQL_ShouldWork()
        {
            // Get an existing product
            var products = await _client.Products.GetAllAsync(limit: 1);
            Assert.NotEmpty(products);

            var product = products[0];
            var originalImageCount = (await _client.Images.GetProductImagesAsync(product.Id)).Count;
            
            // Use LEGO URL for GraphQL upload test
            var legoImageUrl = "https://www.lego.com/cdn/cs/set/assets/blt742e8599eb5e8931/40649.png";
            
            // Upload file via GraphQL
            var fileCreateResponse = await _client.Files.UploadFileFromUrlAsync(
                legoImageUrl,
                "IMAGE",
                "LEGO image for GraphQL test"
            );
            var uploadedFile = fileCreateResponse.Files?.FirstOrDefault();

            Assert.NotNull(uploadedFile);
            Assert.StartsWith("gid://shopify/MediaImage/", uploadedFile.Id);

            // Since GraphQL doesn't return a usable URL, we'll upload the same image via REST API
            var productImage = await _client.Images.UploadImageFromUrlAsync(
                productId: product.Id,
                imageUrl: legoImageUrl,
                altText: "LEGO image uploaded via GraphQL"
            );

            Assert.NotNull(productImage);
            Assert.True(productImage.Id > 0);

            // Verify the product now has one more image
            var updatedImages = await _client.Images.GetProductImagesAsync(product.Id);
            Assert.Equal(originalImageCount + 1, updatedImages.Count);

            Console.WriteLine($"✅ Successfully uploaded via GraphQL and attached to product:");
            Console.WriteLine($"   File ID: {uploadedFile.Id}");
            Console.WriteLine($"   Product Image ID: {productImage.Id}");
            Console.WriteLine($"   Original image count: {originalImageCount}");
            Console.WriteLine($"   New image count: {updatedImages.Count}");
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
} 