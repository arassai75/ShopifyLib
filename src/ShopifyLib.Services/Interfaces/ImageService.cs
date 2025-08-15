using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShopifyLib.Models;
using ShopifyLib.Utils;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Implementation of image-related operations
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly HttpClient _httpClient;
        private readonly ShopifyConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the ImageService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <param name="config">The Shopify configuration.</param>
        public ImageService(HttpClient httpClient, ShopifyConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicyHelper.SnakeCaseLower
            };
        }

        /// <summary>
        /// Uploads an image to a product from a file path.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imagePath">The path to the image file.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the image file is not found.</exception>
        /// <exception cref="HttpRequestException">Thrown when the upload fails.</exception>
        public async Task<ProductImage> UploadImageAsync(long productId, string imagePath, string altText = null, int? position = null)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            using (var stream = System.IO.File.OpenRead(imagePath))
            {
                return await UploadImageAsync(productId, stream, Path.GetFileName(imagePath), altText, position);
            }
        }

        /// <summary>
        /// Uploads an image to a product from a stream.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imageStream">The stream containing the image data.</param>
        /// <param name="fileName">The name of the image file.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the upload fails.</exception>
        public async Task<ProductImage> UploadImageAsync(long productId, Stream imageStream, string fileName, string altText = null, int? position = null)
        {
            // Read the stream into a byte array and base64 encode it
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await imageStream.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }
            var base64Image = Convert.ToBase64String(imageBytes);

            var image = new ProductImage
            {
                ProductId = productId,
                Alt = altText,
                Position = position ?? 1,
                // Shopify expects the property to be named 'attachment' in the JSON
            };

            // Build the JSON payload manually to include the 'attachment' property
            var payload = new
            {
                image = new
                {
                    attachment = base64Image,
                    alt = altText,
                    position = position
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Format("products/{0}/images.json", productId), content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Image upload failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImageResponse>(responseContent, _jsonOptions);
            return result != null ? result.Image : throw new InvalidOperationException("Failed to upload image");
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Uploads an image to a product from a URL.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imageUrl">The URL of the image to upload.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the upload fails.</exception>
        public async Task<ProductImage> UploadImageFromUrlAsync(long productId, string imageUrl, string altText = null, int? position = null)
        {
            var image = new ProductImage
            {
                ProductId = productId,
                Src = imageUrl,
                Alt = altText,
                Position = position ?? 1
            };

            var request = new ProductImageRequest { Image = image };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Format("products/{0}/images.json", productId), content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Image upload failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImageResponse>(responseContent, _jsonOptions);
            return result != null ? result.Image : throw new InvalidOperationException("Failed to upload image from URL");
        }

        /// <summary>
        /// Gets all images for a product.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <returns>A list of product images.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<List<ProductImage>> GetProductImagesAsync(long productId)
        {
            var response = await _httpClient.GetAsync(string.Format("products/{0}/images.json", productId));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImagesResponse>(content, _jsonOptions);
            return result != null ? result.Images : new List<ProductImage>();
        }

        /// <summary>
        /// Gets a specific image for a product.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="imageId">The ID of the image.</param>
        /// <returns>The product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the image is not found.</exception>
        public async Task<ProductImage> GetAsync(long productId, long imageId)
        {
            var response = await _httpClient.GetAsync(string.Format("products/{0}/images/{1}.json", productId, imageId));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImageResponse>(content, _jsonOptions);
            return result != null ? result.Image : throw new InvalidOperationException("Image not found");
        }

        /// <summary>
        /// Updates a product image.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="imageId">The ID of the image to update.</param>
        /// <param name="image">The updated image data.</param>
        /// <returns>The updated product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the update fails.</exception>
        public async Task<ProductImage> UpdateAsync(long productId, long imageId, ProductImage image)
        {
            var request = new ProductImageRequest { Image = image };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(string.Format("products/{0}/images/{1}.json", productId, imageId), content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImageResponse>(responseContent, _jsonOptions);
            return result != null ? result.Image : throw new InvalidOperationException("Failed to update image");
        }

        /// <summary>
        /// Deletes a product image.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="imageId">The ID of the image to delete.</param>
        /// <returns>True if the deletion was successful, false otherwise.</returns>
        public async Task<bool> DeleteAsync(long productId, long imageId)
        {
            var response = await _httpClient.DeleteAsync(string.Format("products/{0}/images/{1}.json", productId, imageId));
            return response.IsSuccessStatusCode;
        }

        // Variant-specific image operations

        /// <summary>
        /// Uploads an image to a product and associates it with specific variants from a file path.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imagePath">The path to the image file.</param>
        /// <param name="variantIds">List of variant IDs to associate with this image.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the image file is not found.</exception>
        /// <exception cref="HttpRequestException">Thrown when the upload fails.</exception>
        public async Task<ProductImage> UploadImageForVariantsAsync(long productId, string imagePath, List<long> variantIds, string altText = null, int? position = null)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            using (var stream = System.IO.File.OpenRead(imagePath))
            {
                return await UploadImageForVariantsAsync(productId, stream, Path.GetFileName(imagePath), variantIds, altText, position);
            }
        }

        /// <summary>
        /// Uploads an image to a product and associates it with specific variants using a stream.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imageStream">The stream containing the image data.</param>
        /// <param name="fileName">The name of the image file.</param>
        /// <param name="variantIds">List of variant IDs to associate with this image.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the upload fails.</exception>
        public async Task<ProductImage> UploadImageForVariantsAsync(long productId, Stream imageStream, string fileName, List<long> variantIds, string altText = null, int? position = null)
        {
            // Read the stream into a byte array and base64 encode it
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await imageStream.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }
            var base64Image = Convert.ToBase64String(imageBytes);

            // Build the JSON payload with variant associations
            var payload = new
            {
                image = new
                {
                    attachment = base64Image,
                    alt = altText,
                    position = position,
                    variant_ids = variantIds
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Format("products/{0}/images.json", productId), content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Image upload failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImageResponse>(responseContent, _jsonOptions);
            return result != null ? result.Image : throw new InvalidOperationException("Failed to upload image");
        }

        /// <summary>
        /// Uploads an image to a product and associates it with specific variants using a URL.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imageUrl">The URL of the image to upload.</param>
        /// <param name="variantIds">List of variant IDs to associate with this image.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the upload fails.</exception>
        public async Task<ProductImage> UploadImageFromUrlForVariantsAsync(long productId, string imageUrl, List<long> variantIds, string altText = null, int? position = null)
        {
            var image = new ProductImage
            {
                ProductId = productId,
                Src = imageUrl,
                Alt = altText,
                Position = position ?? 1,
                VariantIds = variantIds
            };

            var request = new ProductImageRequest { Image = image };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Format("products/{0}/images.json", productId), content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Image upload failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductImageResponse>(responseContent, _jsonOptions);
            return result != null ? result.Image : throw new InvalidOperationException("Failed to upload image from URL");
        }

        /// <summary>
        /// Gets images for a specific variant.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="variantId">The ID of the variant.</param>
        /// <returns>A list of images associated with the variant.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<List<ProductImage>> GetVariantImagesAsync(long productId, long variantId)
        {
            var allImages = await GetProductImagesAsync(productId);
            return allImages.Where(img => img.VariantIds.Contains(variantId)).ToList();
        }

        /// <summary>
        /// Updates an image to associate it with different variants.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="imageId">The ID of the image to update.</param>
        /// <param name="variantIds">List of variant IDs to associate with this image.</param>
        /// <returns>The updated product image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the update fails.</exception>
        public async Task<ProductImage> UpdateImageVariantAssociationsAsync(long productId, long imageId, List<long> variantIds)
        {
            var currentImage = await GetAsync(productId, imageId);
            currentImage.VariantIds = variantIds;
            
            return await UpdateAsync(productId, imageId, currentImage);
        }

        // Helper classes for JSON serialization
        private class ProductImageRequest
        {
            public ProductImage Image { get; set; } = new ProductImage();
        }

        private class ProductImageResponse
        {
            public ProductImage Image { get; set; } = new ProductImage();
        }

        private class ProductImagesResponse
        {
            public List<ProductImage> Images { get; set; } = new List<ProductImage>();
        }
    }
} 