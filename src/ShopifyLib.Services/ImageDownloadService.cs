using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Service for downloading images with proper User-Agent headers
    /// </summary>
    public class ImageDownloadService
    {
        private readonly HttpClient _httpClient;

        public ImageDownloadService()
        {
            _httpClient = new HttpClient();
            
            // Add comprehensive headers to bypass CDN restrictions (especially Akamai)
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/apng,image/*,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "image");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
            _httpClient.DefaultRequestHeaders.Add("DNT", "1");
            
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Downloads an image from a URL with proper User-Agent header
        /// </summary>
        /// <param name="imageUrl">The URL of the image to download</param>
        /// <returns>The image bytes</returns>
        public async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));

            try
            {
                return await _httpClient.GetByteArrayAsync(imageUrl);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to download image from {imageUrl}. The server may require a User-Agent header or the URL may be invalid.", ex);
            }
        }

        /// <summary>
        /// Downloads an image and converts it to base64 data URL format
        /// </summary>
        /// <param name="imageUrl">The URL of the image to download</param>
        /// <param name="contentType">The content type (default: image/jpeg)</param>
        /// <returns>The base64 data URL</returns>
        public async Task<string> DownloadImageAsBase64Async(string imageUrl, string contentType = "image/jpeg")
        {
            var imageBytes = await DownloadImageAsync(imageUrl);
            var base64String = Convert.ToBase64String(imageBytes);
            return $"data:{contentType};base64,{base64String}";
        }

        /// <summary>
        /// Determines if a URL likely requires a User-Agent header
        /// </summary>
        /// <param name="imageUrl">The image URL to check</param>
        /// <returns>True if the URL likely requires a User-Agent header</returns>
        public static bool RequiresUserAgent(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            var lowerUrl = imageUrl.ToLowerInvariant();
            
            // Known domains that often require User-Agent headers
            return lowerUrl.Contains("images.ca") ||
                   lowerUrl.Contains("dynamic.images.ca") ||
                   lowerUrl.Contains("amazonaws.com") ||
                   lowerUrl.Contains("cloudfront.net") ||
                   lowerUrl.Contains("cdn.shopify.com") ||
                   lowerUrl.Contains("myshopify.com") ||
                   lowerUrl.Contains("akamai.net") ||
                   lowerUrl.Contains("akamaized.net");
        }

        /// <summary>
        /// Disposes the underlying HttpClient
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 