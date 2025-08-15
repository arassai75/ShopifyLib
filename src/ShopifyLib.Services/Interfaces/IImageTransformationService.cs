using System.Collections.Generic;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for building Shopify CDN URLs with image transformations
    /// </summary>
    public interface IImageTransformationService
    {
        /// <summary>
        /// Builds a transformed CDN URL with the specified parameters
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="transformations">Transformation parameters to apply</param>
        /// <returns>The transformed CDN URL</returns>
        string BuildTransformedUrl(string baseCdnUrl, ImageTransformations transformations);

        /// <summary>
        /// Creates a thumbnail URL (square image)
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="size">Size in pixels (default: 150)</param>
        /// <param name="crop">Crop mode (default: center)</param>
        /// <returns>The thumbnail URL</returns>
        string CreateThumbnailUrl(string baseCdnUrl, int size = 150, CropMode crop = CropMode.Center);

        /// <summary>
        /// Creates a medium-sized image URL
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="width">Width in pixels (default: 800)</param>
        /// <param name="height">Height in pixels (default: 600)</param>
        /// <param name="crop">Crop mode (default: center)</param>
        /// <returns>The medium image URL</returns>
        string CreateMediumUrl(string baseCdnUrl, int width = 800, int height = 600, CropMode crop = CropMode.Center);

        /// <summary>
        /// Creates a large image URL
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="width">Width in pixels (default: 1200)</param>
        /// <param name="height">Height in pixels (default: 800)</param>
        /// <param name="crop">Crop mode (default: center)</param>
        /// <returns>The large image URL</returns>
        string CreateLargeUrl(string baseCdnUrl, int width = 1200, int height = 800, CropMode crop = CropMode.Center);

        /// <summary>
        /// Creates a WebP format URL for better performance
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="quality">Quality (1-100, default: 85)</param>
        /// <returns>The WebP URL</returns>
        string CreateWebPUrl(string baseCdnUrl, int quality = 85);

        /// <summary>
        /// Creates multiple transformation URLs for responsive design
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <returns>Dictionary of transformation names to URLs</returns>
        Dictionary<string, string> CreateResponsiveUrls(string baseCdnUrl);

        /// <summary>
        /// Creates a high-quality URL for original viewing
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="quality">Quality (1-100, default: 100)</param>
        /// <returns>The high-quality URL</returns>
        string CreateHighQualityUrl(string baseCdnUrl, int quality = 100);
    }
} 