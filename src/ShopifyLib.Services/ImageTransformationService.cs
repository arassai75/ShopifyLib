using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Service for building Shopify CDN URLs with image transformations
    /// </summary>
    public class ImageTransformationService : IImageTransformationService
    {
        /// <summary>
        /// Builds a transformed CDN URL with the specified parameters
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="transformations">Transformation parameters to apply</param>
        /// <returns>The transformed CDN URL</returns>
        public string BuildTransformedUrl(string baseCdnUrl, ImageTransformations transformations)
        {
            if (string.IsNullOrEmpty(baseCdnUrl))
                throw new ArgumentException("Base CDN URL cannot be null or empty", nameof(baseCdnUrl));

            if (transformations == null)
                return baseCdnUrl;

            var parameters = new List<string>();

            // Add width parameter
            if (transformations.Width.HasValue)
                parameters.Add($"width={transformations.Width.Value}");

            // Add height parameter
            if (transformations.Height.HasValue)
                parameters.Add($"height={transformations.Height.Value}");

            // Add crop parameter
            if (transformations.Crop.HasValue)
                parameters.Add($"crop={GetEnumDescription(transformations.Crop.Value)}");

            // Add format parameter
            if (transformations.Format.HasValue)
                parameters.Add($"format={GetEnumDescription(transformations.Format.Value)}");

            // Add quality parameter
            if (transformations.Quality.HasValue)
            {
                var quality = Math.Max(1, Math.Min(100, transformations.Quality.Value));
                parameters.Add($"quality={quality}");
            }

            // Add scale parameter
            if (transformations.Scale.HasValue)
                parameters.Add($"scale={transformations.Scale.Value}");

            // Return original URL if no transformations
            if (parameters.Count == 0)
                return baseCdnUrl;

            // Build the final URL
            var separator = baseCdnUrl.Contains('?') ? '&' : '?';
            return $"{baseCdnUrl}{separator}{string.Join("&", parameters)}";
        }

        /// <summary>
        /// Creates a thumbnail URL (square image)
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="size">Size in pixels (default: 150)</param>
        /// <param name="crop">Crop mode (default: center)</param>
        /// <returns>The thumbnail URL</returns>
        public string CreateThumbnailUrl(string baseCdnUrl, int size = 150, CropMode crop = CropMode.Center)
        {
            var transformations = new ImageTransformations
            {
                Width = size,
                Height = size,
                Crop = crop
            };
            return BuildTransformedUrl(baseCdnUrl, transformations);
        }

        /// <summary>
        /// Creates a medium-sized image URL
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="width">Width in pixels (default: 800)</param>
        /// <param name="height">Height in pixels (default: 600)</param>
        /// <param name="crop">Crop mode (default: center)</param>
        /// <returns>The medium image URL</returns>
        public string CreateMediumUrl(string baseCdnUrl, int width = 800, int height = 600, CropMode crop = CropMode.Center)
        {
            var transformations = new ImageTransformations
            {
                Width = width,
                Height = height,
                Crop = crop
            };
            return BuildTransformedUrl(baseCdnUrl, transformations);
        }

        /// <summary>
        /// Creates a large image URL
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="width">Width in pixels (default: 1200)</param>
        /// <param name="height">Height in pixels (default: 800)</param>
        /// <param name="crop">Crop mode (default: center)</param>
        /// <returns>The large image URL</returns>
        public string CreateLargeUrl(string baseCdnUrl, int width = 1200, int height = 800, CropMode crop = CropMode.Center)
        {
            var transformations = new ImageTransformations
            {
                Width = width,
                Height = height,
                Crop = crop
            };
            return BuildTransformedUrl(baseCdnUrl, transformations);
        }

        /// <summary>
        /// Creates a WebP format URL for better performance
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="quality">Quality (1-100, default: 85)</param>
        /// <returns>The WebP URL</returns>
        public string CreateWebPUrl(string baseCdnUrl, int quality = 85)
        {
            var transformations = new ImageTransformations
            {
                Format = ImageFormat.WebP,
                Quality = quality
            };
            return BuildTransformedUrl(baseCdnUrl, transformations);
        }

        /// <summary>
        /// Creates multiple transformation URLs for responsive design
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <returns>Dictionary of transformation names to URLs</returns>
        public Dictionary<string, string> CreateResponsiveUrls(string baseCdnUrl)
        {
            return new Dictionary<string, string>
            {
                ["thumbnail"] = CreateThumbnailUrl(baseCdnUrl, 150),
                ["small"] = CreateThumbnailUrl(baseCdnUrl, 300),
                ["medium"] = CreateMediumUrl(baseCdnUrl, 800, 600),
                ["large"] = CreateLargeUrl(baseCdnUrl, 1200, 800),
                ["webp"] = CreateWebPUrl(baseCdnUrl, 85),
                ["original"] = baseCdnUrl
            };
        }

        /// <summary>
        /// Creates a high-quality URL for original viewing
        /// </summary>
        /// <param name="baseCdnUrl">The base Shopify CDN URL</param>
        /// <param name="quality">Quality (1-100, default: 100)</param>
        /// <returns>The high-quality URL</returns>
        public string CreateHighQualityUrl(string baseCdnUrl, int quality = 100)
        {
            var transformations = new ImageTransformations
            {
                Quality = quality
            };
            return BuildTransformedUrl(baseCdnUrl, transformations);
        }

        /// <summary>
        /// Gets the description attribute value from an enum
        /// </summary>
        /// <param name="enumValue">The enum value</param>
        /// <returns>The description string</returns>
        private static string GetEnumDescription(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? enumValue.ToString().ToLowerInvariant();
        }
    }
} 