using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Xunit;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class HybridApproachTest : IDisposable
    {
        private readonly ShopifyClient _client;
        private readonly HttpClient _httpClient;

        public HybridApproachTest()
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
        public async Task HybridApproach_IndigoURL_GraphQLAndCDN()
        {
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419407182/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - Hybrid Approach Test";

            Console.WriteLine("=== HYBRID APPROACH TEST ===");
            Console.WriteLine("Combining GraphQL file management with immediate CDN URL access");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            // Step 1: Upload via GraphQL
            var fileInput = new FileCreateInput
            {
                OriginalSource = imageUrl,
                ContentType = FileContentType.Image,
                Alt = altText
            };
            var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
            Assert.NotNull(graphqlResponse);
            Assert.NotNull(graphqlResponse.Files);
            Assert.NotEmpty(graphqlResponse.Files);
            Assert.Empty(graphqlResponse.UserErrors);
            var graphqlFile = graphqlResponse.Files[0];
            Console.WriteLine($"✅ GraphQL file created: {graphqlFile.Id}");

            // Step 2: Upload same image via REST to get immediate CDN URL
            var tempProduct = new Product
            {
                Title = $"Temp Product {DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = "draft",
                Published = false
            };
            var createdProduct = await _client.Products.CreateAsync(tempProduct);
            Console.WriteLine($"✅ Temporary product created: {createdProduct.Id}");
            string cdnUrl = null;
            try
            {
                var restImage = await _client.Images.UploadImageFromUrlAsync(
                    createdProduct.Id,
                    imageUrl,
                    altText,
                    1
                );
                cdnUrl = restImage.Src;
                Console.WriteLine($"✅ REST image uploaded: {restImage.Id}");
                Console.WriteLine($"🌐 CDN URL obtained: {cdnUrl}");
            }
            finally
            {
                Console.WriteLine("🧹 Cleaning up temporary product...");
                await _client.Products.DeleteAsync(createdProduct.Id);
                Console.WriteLine("✅ Temporary product deleted");
            }

            // Validate results
            Assert.False(string.IsNullOrEmpty(graphqlFile.Id), "GraphQL File ID should not be empty");
            Assert.False(string.IsNullOrEmpty(cdnUrl), "CDN URL should not be empty");
            Assert.StartsWith("https://cdn.shopify.com", cdnUrl);

            // Test CDN URL accessibility
            Console.WriteLine("🔄 Testing CDN URL accessibility...");
            var isAccessible = await TestUrlAccessibilityAsync(cdnUrl);
            Console.WriteLine(isAccessible ? "✅ CDN URL is accessible!" : "❌ CDN URL returns 404");
            Assert.True(isAccessible, "CDN URL should be accessible immediately");

            // Summary
            Console.WriteLine();
            Console.WriteLine("=== HYBRID APPROACH BENEFITS ===");
            Console.WriteLine($"GraphQL File ID: {graphqlFile.Id}");
            Console.WriteLine($"Immediate CDN URL: {cdnUrl}");
            Console.WriteLine("✅ File is managed in Shopify media library");
            Console.WriteLine("✅ CDN URL is available instantly");
            Console.WriteLine("✅ Temporary product is cleaned up");
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