using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Service for fixing Shopify CDN URL 404 issues with multiple fallback strategies
    /// </summary>
    public class CDNUrlFixService : ICDNUrlFixService
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphQLService _graphQLService;

        public CDNUrlFixService(IGraphQLService graphQLService)
        {
            _graphQLService = graphQLService ?? throw new ArgumentNullException(nameof(graphQLService));
            
            // Initialize HTTP client with proper headers
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/apng,image/*,*/*;q=0.8");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Fixes a CDN URL that returns 404 by trying multiple strategies
        /// </summary>
        /// <param name="originalCdnUrl">The original CDN URL that returns 404</param>
        /// <param name="fileId">The Shopify file ID (optional, for URL construction)</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>A working CDN URL or the original URL if all fixes fail</returns>
        public async Task<string> FixCDNUrlAsync(string originalCdnUrl, string fileId = null, int maxRetries = 3)
        {
            if (string.IsNullOrEmpty(originalCdnUrl))
                return originalCdnUrl;

            // Test original URL first
            if (await IsUrlAccessibleAsync(originalCdnUrl))
                return originalCdnUrl;

            Console.WriteLine($"🔧 CDN URL 404 detected: {originalCdnUrl}");
            Console.WriteLine("🔄 Attempting to fix CDN URL...");

            // Strategy 1: Remove version parameter
            var urlWithoutVersion = RemoveVersionParameter(originalCdnUrl);
            if (await IsUrlAccessibleAsync(urlWithoutVersion))
            {
                Console.WriteLine("✅ Fixed: Removed version parameter");
                return urlWithoutVersion;
            }

            // Strategy 2: Try alternative version formats
            var alternativeVersions = GenerateAlternativeVersions(originalCdnUrl);
            foreach (var versionUrl in alternativeVersions)
            {
                if (await IsUrlAccessibleAsync(versionUrl))
                {
                    Console.WriteLine("✅ Fixed: Alternative version format");
                    return versionUrl;
                }
            }

            // Strategy 3: Try alternative URL patterns
            if (!string.IsNullOrEmpty(fileId))
            {
                var alternativePatterns = GenerateAlternativePatterns(originalCdnUrl, fileId);
                foreach (var patternUrl in alternativePatterns)
                {
                    if (await IsUrlAccessibleAsync(patternUrl))
                    {
                        Console.WriteLine("✅ Fixed: Alternative URL pattern");
                        return patternUrl;
                    }
                }
            }

            // Strategy 4: Query GraphQL for updated URL
            if (!string.IsNullOrEmpty(fileId))
            {
                var graphqlUrl = await GetUpdatedCDNUrlFromGraphQLAsync(fileId);
                if (!string.IsNullOrEmpty(graphqlUrl) && await IsUrlAccessibleAsync(graphqlUrl))
                {
                    Console.WriteLine("✅ Fixed: Updated URL from GraphQL");
                    return graphqlUrl;
                }
            }

            // Strategy 5: Retry with exponential backoff
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                Console.WriteLine($"⏳ Retry attempt {attempt}/{maxRetries} in {delay.TotalSeconds} seconds...");
                await Task.Delay(delay);

                if (await IsUrlAccessibleAsync(originalCdnUrl))
                {
                    Console.WriteLine("✅ Fixed: URL became accessible after retry");
                    return originalCdnUrl;
                }
            }

            Console.WriteLine("❌ All CDN URL fixes failed");
            return originalCdnUrl;
        }

        /// <summary>
        /// Validates and fixes multiple CDN URLs
        /// </summary>
        /// <param name="cdnUrls">List of CDN URLs to validate and fix</param>
        /// <param name="fileIds">Corresponding file IDs (optional)</param>
        /// <returns>Dictionary of original URLs to fixed URLs</returns>
        public async Task<Dictionary<string, string>> FixMultipleCDNUrlsAsync(
            List<string> cdnUrls, 
            List<string> fileIds = null)
        {
            var results = new Dictionary<string, string>();

            for (int i = 0; i < cdnUrls.Count; i++)
            {
                var originalUrl = cdnUrls[i];
                var fileId = fileIds?.Count > i ? fileIds[i] : null;

                var fixedUrl = await FixCDNUrlAsync(originalUrl, fileId);
                results[originalUrl] = fixedUrl;
            }

            return results;
        }

        /// <summary>
        /// Gets a working CDN URL with fallback strategies
        /// </summary>
        /// <param name="cdnUrl">Primary CDN URL</param>
        /// <param name="fallbackUrl">Fallback URL (e.g., original source URL)</param>
        /// <param name="fileId">Shopify file ID for URL construction</param>
        /// <returns>A working URL (CDN or fallback)</returns>
        public async Task<string> GetWorkingUrlWithFallbackAsync(
            string cdnUrl, 
            string fallbackUrl, 
            string fileId = null)
        {
            // Try to fix CDN URL first
            var fixedCdnUrl = await FixCDNUrlAsync(cdnUrl, fileId);
            
            if (await IsUrlAccessibleAsync(fixedCdnUrl))
            {
                return fixedCdnUrl;
            }

            // If CDN URL still doesn't work, use fallback
            Console.WriteLine($"⚠️  CDN URL not accessible, using fallback: {fallbackUrl}");
            return fallbackUrl;
        }

        /// <summary>
        /// Tests if a URL is accessible
        /// </summary>
        /// <param name="url">URL to test</param>
        /// <returns>True if URL is accessible, false otherwise</returns>
        public async Task<bool> IsUrlAccessibleAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            try
            {
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes version parameter from CDN URL
        /// </summary>
        /// <param name="url">CDN URL with version parameter</param>
        /// <returns>CDN URL without version parameter</returns>
        private string RemoveVersionParameter(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            
            // Remove ?v= parameter
            var regex = new Regex(@"\?v=\d+(&|$)");
            var result = regex.Replace(url, "");
            
            // Clean up trailing ? if no other parameters
            if (result.EndsWith("?"))
            {
                result = result.TrimEnd('?');
            }
            
            return result;
        }

        /// <summary>
        /// Generates alternative version formats for CDN URL
        /// </summary>
        /// <param name="url">Original CDN URL</param>
        /// <returns>List of alternative version URLs</returns>
        private List<string> GenerateAlternativeVersions(string url)
        {
            var alternatives = new List<string>();
            
            if (string.IsNullOrEmpty(url)) return alternatives;

            // Try different version timestamps
            var timestamps = new[]
            {
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300).ToString(), // 5 minutes ago
                (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300).ToString(), // 5 minutes from now
                "1", // Simple version
                "0"  // No version
            };

            foreach (var timestamp in timestamps)
            {
                var alternative = url.Contains("?v=") 
                    ? url.Replace(Regex.Match(url, @"\?v=\d+").Value, $"?v={timestamp}")
                    : url + (url.Contains("?") ? "&" : "?") + $"v={timestamp}";
                
                alternatives.Add(alternative);
            }

            return alternatives;
        }

        /// <summary>
        /// Generates alternative URL patterns
        /// </summary>
        /// <param name="url">Original CDN URL</param>
        /// <param name="fileId">Shopify file ID</param>
        /// <returns>List of alternative URL patterns</returns>
        private List<string> GenerateAlternativePatterns(string url, string fileId)
        {
            var alternatives = new List<string>();
            
            if (string.IsNullOrEmpty(url)) return alternatives;

            // Extract base URL without parameters
            var baseUrl = url.Split('?')[0];
            
            // Try different URL patterns
            alternatives.Add(baseUrl); // No parameters
            
            // Try with different file extensions
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            foreach (var ext in extensions)
            {
                var urlWithExt = baseUrl;
                if (!urlWithExt.EndsWith(ext))
                {
                    urlWithExt = urlWithExt.Replace(".jpg", ext).Replace(".jpeg", ext).Replace(".png", ext).Replace(".webp", ext);
                }
                alternatives.Add(urlWithExt);
            }

            // Try with image ID in URL
            alternatives.Add($"{baseUrl}?id={fileId}");
            alternatives.Add($"{baseUrl}?image_id={fileId}");

            return alternatives;
        }

        /// <summary>
        /// Gets updated CDN URL from GraphQL query
        /// </summary>
        /// <param name="fileId">Shopify file ID</param>
        /// <returns>Updated CDN URL or null if not found</returns>
        private async Task<string> GetUpdatedCDNUrlFromGraphQLAsync(string fileId)
        {
            try
            {
                var fileQuery = @"
                    query getFile($id: ID!) {
                        node(id: $id) {
                            ... on MediaImage {
                                id
                                fileStatus
                                image {
                                    url
                                    originalSrc
                                    transformedSrc
                                    src
                                }
                            }
                        }
                    }";

                var queryResponse = await _graphQLService.ExecuteQueryAsync(fileQuery, new { id = fileId });
                var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(queryResponse);
                var node = jsonResponse["data"]?["node"];

                if (node?["image"] != null)
                {
                    var image = node["image"];
                    // Try different URL fields in order of preference
                    return image["url"]?.ToString() ?? 
                           image["src"]?.ToString() ?? 
                           image["originalSrc"]?.ToString() ?? 
                           image["transformedSrc"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GraphQL query failed: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Interface for CDN URL fix service
    /// </summary>
    public interface ICDNUrlFixService : IDisposable
    {
        /// <summary>
        /// Fixes a CDN URL that returns 404
        /// </summary>
        /// <param name="originalCdnUrl">The original CDN URL that returns 404</param>
        /// <param name="fileId">The Shopify file ID (optional)</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>A working CDN URL or the original URL if all fixes fail</returns>
        Task<string> FixCDNUrlAsync(string originalCdnUrl, string fileId = null, int maxRetries = 3);

        /// <summary>
        /// Validates and fixes multiple CDN URLs
        /// </summary>
        /// <param name="cdnUrls">List of CDN URLs to validate and fix</param>
        /// <param name="fileIds">Corresponding file IDs (optional)</param>
        /// <returns>Dictionary of original URLs to fixed URLs</returns>
        Task<Dictionary<string, string>> FixMultipleCDNUrlsAsync(List<string> cdnUrls, List<string> fileIds = null);

        /// <summary>
        /// Gets a working CDN URL with fallback strategies
        /// </summary>
        /// <param name="cdnUrl">Primary CDN URL</param>
        /// <param name="fallbackUrl">Fallback URL (e.g., original source URL)</param>
        /// <param name="fileId">Shopify file ID for URL construction</param>
        /// <returns>A working URL (CDN or fallback)</returns>
        Task<string> GetWorkingUrlWithFallbackAsync(string cdnUrl, string fallbackUrl, string fileId = null);

        /// <summary>
        /// Tests if a URL is accessible
        /// </summary>
        /// <param name="url">URL to test</param>
        /// <returns>True if URL is accessible, false otherwise</returns>
        Task<bool> IsUrlAccessibleAsync(string url);
    }
} 