using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Xunit;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class GraphQLCDNTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly HttpClient _httpClient;

        public GraphQLCDNTest()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var shopifyConfig = configuration.GetShopifyConfig();
            if (!shopifyConfig.IsValid())
                throw new InvalidOperationException("Shopify configuration is not valid. Please check your appsettings.json or environment variables.");

            _client = new ShopifyClient(shopifyConfig);
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/apng,image/*,*/*;q=0.8");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        [Fact]
        public async Task UploadImage_GraphQL_CDNUrl_BecomesAccessible()
        {
            var imageUrl = "https://dynamic.images.ca/v1/gifts/gifts/673419407182/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = " Gift Image - GraphQL CDN Test";

            Console.WriteLine("=== GRAPHQL  IMAGE UPLOAD - CDN URL TEST ===");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            // Step 1: Upload image via GraphQL
            var fileInput = new FileCreateInput
            {
                OriginalSource = imageUrl,
                ContentType = FileContentType.Image,
                Alt = altText
            };
            var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
            Assert.NotNull(response);
            Assert.NotNull(response.Files);
            Assert.NotEmpty(response.Files);
            Assert.Empty(response.UserErrors);
            var uploadedFile = response.Files[0];
            Console.WriteLine($"✅ Uploaded file ID: {uploadedFile.Id}");

            // Step 2: Poll for CDN URL
            var maxWait = TimeSpan.FromMinutes(3);
            var interval = TimeSpan.FromSeconds(10);
            var waited = TimeSpan.Zero;
            string cdnUrl = null;
            while (waited < maxWait)
            {
                await Task.Delay(interval);
                waited += interval;
                cdnUrl = await QueryFileForCDNUrlAsync(uploadedFile.Id);
                if (!string.IsNullOrEmpty(cdnUrl))
                {
                    Console.WriteLine($"✅ CDN URL available after {waited.TotalSeconds:F0} seconds: {cdnUrl}");
                    break;
                }
                Console.WriteLine($"⏳ Waiting for CDN URL... {waited.TotalSeconds:F0}s");
            }
            Assert.False(string.IsNullOrEmpty(cdnUrl), "CDN URL was not available after waiting");

            // Step 3: Test CDN URL accessibility
            var isAccessible = await TestUrlAccessibilityAsync(cdnUrl);
            Console.WriteLine(isAccessible ? "✅ CDN URL is accessible!" : "❌ CDN URL returns 404");
            Assert.True(isAccessible, "CDN URL should eventually be accessible (not 404)");
        }

        private async Task<string> QueryFileForCDNUrlAsync(string fileId)
        {
            var fileQuery = @"
                query getFile($id: ID!) {
                    node(id: $id) {
                        ... on MediaImage {
                            id
                            fileStatus
                            image {
                                url
                                src
                                originalSrc
                                transformedSrc
                            }
                        }
                    }
                }";
            var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, new { id = fileId });
            var parsed = JsonConvert.DeserializeObject<dynamic>(queryResponse);
            var node = parsed?.data?.node;
            if (node?.image != null)
            {
                return node.image.url?.ToString() ?? node.image.src?.ToString() ?? node.image.originalSrc?.ToString() ?? node.image.transformedSrc?.ToString();
            }
            return null;
        }

        private async Task<bool> TestUrlAccessibilityAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            try
            {
                var resp = await _httpClient.GetAsync(url);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _httpClient?.Dispose();
        }
    }
} 