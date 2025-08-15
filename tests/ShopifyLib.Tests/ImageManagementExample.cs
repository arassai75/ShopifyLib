using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using FileModel = ShopifyLib.Models.File;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Example demonstrating how to manage images for existing products
    /// </summary>
    public class ImageManagementExample
    {
        private readonly ShopifyClient _client;

        public ImageManagementExample(ShopifyClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Demonstrates various ways to manage images for existing products
        /// </summary>
        public async Task ManageProductImagesAsync(long existingProductId)
        {
            Console.WriteLine($"=== Managing Images for Product {existingProductId} ===");

            // 1. Get current images
            var currentImages = await _client.Images.GetProductImagesAsync(existingProductId);
            Console.WriteLine($"Current images: {currentImages.Count}");

            foreach (var img in currentImages)
            {
                Console.WriteLine($"  - Image {img.Id}: {img.Src} (Position: {img.Position})");
            }

            // 2. Add a new image to the product
            Console.WriteLine("\n--- Adding New Image ---");
            try
            {
                var newImage = await _client.Images.UploadImageFromUrlAsync(
                    productId: existingProductId,
                    imageUrl: "https://httpbin.org/image/png",
                    altText: "New product image added via API",
                    position: currentImages.Count + 1
                );

                Console.WriteLine($"✅ Added new image: {newImage.Id}");
                Console.WriteLine($"   URL: {newImage.Src}");
                Console.WriteLine($"   Position: {newImage.Position}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to add new image: {ex.Message}");
            }

            // 3. Update an existing image
            Console.WriteLine("\n--- Updating Existing Image ---");
            if (currentImages.Any())
            {
                var imageToUpdate = currentImages.First();
                imageToUpdate.Alt = $"Updated alt text at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

                try
                {
                    var updatedImage = await _client.Images.UpdateAsync(
                        productId: existingProductId,
                        imageId: imageToUpdate.Id,
                        image: imageToUpdate
                    );

                    Console.WriteLine($"✅ Updated image {updatedImage.Id}");
                    Console.WriteLine($"   New alt text: {updatedImage.Alt}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to update image: {ex.Message}");
                }
            }

            // 4. Replace an image (delete + upload)
            Console.WriteLine("\n--- Replacing Image ---");
            if (currentImages.Count > 1)
            {
                var imageToReplace = currentImages[1]; // Second image
                var originalPosition = imageToReplace.Position;

                try
                {
                    // Delete the old image
                    await _client.Images.DeleteAsync(existingProductId, imageToReplace.Id);
                    Console.WriteLine($"🗑️ Deleted image {imageToReplace.Id}");

                    // Upload replacement image
                    var replacementImage = await _client.Images.UploadImageFromUrlAsync(
                        productId: existingProductId,
                        imageUrl: "https://httpbin.org/image/jpeg",
                        altText: "Replacement image",
                        position: originalPosition
                    );

                    Console.WriteLine($"✅ Replaced with new image: {replacementImage.Id}");
                    Console.WriteLine($"   Position maintained: {replacementImage.Position}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to replace image: {ex.Message}");
                }
            }

            // 5. Reorder images
            Console.WriteLine("\n--- Reordering Images ---");
            var updatedImages = await _client.Images.GetProductImagesAsync(existingProductId);
            
            for (int i = 0; i < updatedImages.Count; i++)
            {
                var img = updatedImages[i];
                img.Position = i + 1; // Reorder positions

                try
                {
                    await _client.Images.UpdateAsync(existingProductId, img.Id, img);
                    Console.WriteLine($"✅ Updated position of image {img.Id} to {img.Position}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to update position of image {img.Id}: {ex.Message}");
                }
            }

            // 6. Final state
            Console.WriteLine("\n--- Final Image State ---");
            var finalImages = await _client.Images.GetProductImagesAsync(existingProductId);
            Console.WriteLine($"Total images: {finalImages.Count}");

            foreach (var img in finalImages.OrderBy(i => i.Position))
            {
                Console.WriteLine($"  {img.Position}. Image {img.Id}: {img.Src}");
                Console.WriteLine($"     Alt: {img.Alt}");
            }
        }

        /// <summary>
        /// Example of uploading a local image file to an existing product
        /// </summary>
        public async Task UploadLocalImageAsync(long existingProductId, string localImagePath)
        {
            Console.WriteLine($"\n=== Uploading Local Image to Product {existingProductId} ===");

            if (!System.IO.File.Exists(localImagePath))
            {
                Console.WriteLine($"❌ File not found: {localImagePath}");
                return;
            }

            try
            {
                var newImage = await _client.Images.UploadImageAsync(
                    productId: existingProductId,
                    imagePath: localImagePath,
                    altText: $"Local image: {System.IO.Path.GetFileName(localImagePath)}"
                );

                Console.WriteLine($"✅ Successfully uploaded local image:");
                Console.WriteLine($"   Image ID: {newImage.Id}");
                Console.WriteLine($"   URL: {newImage.Src}");
                Console.WriteLine($"   Position: {newImage.Position}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to upload local image: {ex.Message}");
            }
        }

        /// <summary>
        /// Example of using GraphQL file upload for more control
        /// </summary>
        public async Task UploadImageViaGraphQLAsync(long existingProductId, string imageUrl = null)
        {
            Console.WriteLine($"\n=== Uploading Image via GraphQL to Product {existingProductId} ===");

            // Use provided URL or default to a real image URL
            var urlToUse = imageUrl ?? "https://httpbin.org/image/webp";

            try
            {
                // Step 1: Upload file via GraphQL
                var fileCreateResponse = await _client.Files.UploadFileFromUrlAsync(
                    urlToUse,
                    ShopifyLib.Models.FileContentType.Image,
                    "GraphQL uploaded image"
                );
                var uploadedFile = fileCreateResponse.Files?.FirstOrDefault();
                
                if (uploadedFile != null)
                {
                    Console.WriteLine($"✅ File uploaded via GraphQL:");
                    Console.WriteLine($"   File ID: {uploadedFile.Id}");
                    Console.WriteLine($"   Status: {uploadedFile.FileStatus}");

                    // Step 2: Attach to product via REST API using the original URL
                    // (GraphQL doesn't return a usable URL for product attachment)
                    var productImage = await _client.Images.UploadImageFromUrlAsync(
                        productId: existingProductId,
                        imageUrl: urlToUse,
                        altText: "Image uploaded via GraphQL"
                    );

                    Console.WriteLine($"✅ Image attached to product:");
                    Console.WriteLine($"   Product Image ID: {productImage.Id}");
                    Console.WriteLine($"   Position: {productImage.Position}");
                }
                else
                {
                    Console.WriteLine("❌ No file returned from GraphQL upload");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to upload via GraphQL: {ex.Message}");
            }
        }
    }
} 