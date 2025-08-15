using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for image-related operations
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Uploads an image to a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="imagePath">The path to the image file</param>
        /// <param name="altText">Optional alt text for the image</param>
        /// <param name="position">Optional position for the image</param>
        /// <returns>The uploaded product image</returns>
        Task<ProductImage> UploadImageAsync(long productId, string imagePath, string altText = null, int? position = null);

        /// <summary>
        /// Uploads an image to a product using a stream
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="imageStream">The image stream</param>
        /// <param name="fileName">The file name</param>
        /// <param name="altText">Optional alt text for the image</param>
        /// <param name="position">Optional position for the image</param>
        /// <returns>The uploaded product image</returns>
        Task<ProductImage> UploadImageAsync(long productId, Stream imageStream, string fileName, string altText = null, int? position = null);

        /// <summary>
        /// Uploads an image using a URL
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="imageUrl">The URL of the image</param>
        /// <param name="altText">Optional alt text for the image</param>
        /// <param name="position">Optional position for the image</param>
        /// <returns>The uploaded product image</returns>
        Task<ProductImage> UploadImageFromUrlAsync(long productId, string imageUrl, string altText = null, int? position = null);

        /// <summary>
        /// Gets all images for a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <returns>List of product images</returns>
        Task<List<ProductImage>> GetProductImagesAsync(long productId);

        /// <summary>
        /// Gets a specific product image
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="imageId">The image ID</param>
        /// <returns>The product image</returns>
        Task<ProductImage> GetAsync(long productId, long imageId);

        /// <summary>
        /// Updates a product image
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="imageId">The image ID</param>
        /// <param name="image">The updated image data</param>
        /// <returns>The updated product image</returns>
        Task<ProductImage> UpdateAsync(long productId, long imageId, ProductImage image);

        /// <summary>
        /// Deletes a product image
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="imageId">The image ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAsync(long productId, long imageId);

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
        Task<ProductImage> UploadImageForVariantsAsync(long productId, string imagePath, List<long> variantIds, string altText = null, int? position = null);

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
        Task<ProductImage> UploadImageForVariantsAsync(long productId, Stream imageStream, string fileName, List<long> variantIds, string altText = null, int? position = null);

        /// <summary>
        /// Uploads an image to a product and associates it with specific variants using a URL.
        /// </summary>
        /// <param name="productId">The ID of the product to upload the image to.</param>
        /// <param name="imageUrl">The URL of the image to upload.</param>
        /// <param name="variantIds">List of variant IDs to associate with this image.</param>
        /// <param name="altText">Optional alt text for the image.</param>
        /// <param name="position">Optional position of the image in the product's image list.</param>
        /// <returns>The uploaded product image.</returns>
        Task<ProductImage> UploadImageFromUrlForVariantsAsync(long productId, string imageUrl, List<long> variantIds, string altText = null, int? position = null);

        /// <summary>
        /// Gets images for a specific variant.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="variantId">The ID of the variant.</param>
        /// <returns>A list of images associated with the variant.</returns>
        Task<List<ProductImage>> GetVariantImagesAsync(long productId, long variantId);

        /// <summary>
        /// Updates an image to associate it with different variants.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="imageId">The ID of the image to update.</param>
        /// <param name="variantIds">List of variant IDs to associate with this image.</param>
        /// <returns>The updated product image.</returns>
        Task<ProductImage> UpdateImageVariantAssociationsAsync(long productId, long imageId, List<long> variantIds);
    }
} 