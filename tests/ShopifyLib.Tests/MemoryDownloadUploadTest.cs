using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Test for downloading images to memory only (no upload)
    /// </summary>
    [IntegrationTest]
    public class MemoryDownloadOnlyTest
    {
        [Fact(DisplayName = "Download Indigo Image to Memory - No Upload")] 
        public async Task DownloadIndigoImageToMemory_Only()
        {
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg";
            Console.WriteLine("=== MEMORY DOWNLOAD ONLY TEST ===");
            Console.WriteLine($"This test demonstrates downloading to memory only (no upload)");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine("\U0001F4E5 Step 1: Downloading image to memory...");

            using var httpClient = new HttpClient();
            
            // Add comprehensive headers to bypass Akamai restrictions
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/apng,image/*,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "image");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
            httpClient.DefaultRequestHeaders.Add("DNT", "1");
            
            // Set BaseAddress for Shopify GraphQL
            httpClient.BaseAddress = new Uri("https://your-shop.myshopify.com/admin/api/2025-04/");
            
            // Add Shopify authentication header
            httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", "your-access-token-here");

            // Set a reasonable timeout
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            
            Console.WriteLine($"User-Agent: {httpClient.DefaultRequestHeaders.UserAgent}");
            Console.WriteLine($"Accept: {httpClient.DefaultRequestHeaders.Accept}");
            
            var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            Console.WriteLine($"✅ Download successful! Size: {imageBytes.Length} bytes");
            Console.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            Console.WriteLine("   Image downloaded to memory successfully");
            
            // Optionally, you could save the image to disk for manual inspection
            // System.IO.File.WriteAllBytes("downloaded-indigo-image.jpg", imageBytes);
            Console.WriteLine("\nYou can now manually upload this image to Shopify or inspect it.");
        }

        [Fact(DisplayName = "Download Indigo Image and Staged Upload to Shopify")] 
        public async Task DownloadAndStagedUploadToShopify()
        {
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg";
            Console.WriteLine("=== DOWNLOAD AND STAGED UPLOAD TO SHOPIFY TEST ===");
            Console.WriteLine($"Image URL: {imageUrl}");
            
            // Load configuration
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            var shopifyConfig = configuration.GetSection("Shopify").Get<ShopifyLib.Models.ShopifyConfig>();
            if (shopifyConfig == null)
                throw new InvalidOperationException("Shopify configuration not found in appsettings.json");

            // Step 1: Download image
            using var downloadClient = new HttpClient();
            downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            downloadClient.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/apng,image/*,*/*;q=0.8");
            downloadClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            downloadClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            downloadClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            downloadClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            downloadClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "image");
            downloadClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
            downloadClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
            downloadClient.DefaultRequestHeaders.Add("DNT", "1");
            downloadClient.Timeout = TimeSpan.FromSeconds(60);

            var response = await downloadClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            Console.WriteLine($"✅ Downloaded! Size: {imageBytes.Length} bytes");
            Console.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            
            // Use the actual content type from the response (Indigo serves WebP even with .jpg URL)
            var mimeType = response.Content.Headers.ContentType?.ToString() ?? "image/webp";
            var extension = mimeType.Contains("webp") ? ".webp" : ".jpg";
            var fileName = $"indigo-image-{DateTime.Now:yyyyMMdd-HHmmss}{extension}";

            // Step 2: Staged upload to Shopify using StagedUploadService
            using var shopifyClient = new HttpClient();
            shopifyClient.BaseAddress = new Uri("https://your-shop.myshopify.com/admin/api/2025-04/");
            shopifyClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", "your-access-token-here");
            shopifyClient.Timeout = TimeSpan.FromSeconds(60);
            var graphQLService = new ShopifyLib.Services.GraphQLService(shopifyClient, shopifyConfig);
            var stagedUploadService = new ShopifyLib.Services.StagedUploadService(graphQLService, shopifyClient);

            var fileCreateResponse = await stagedUploadService.UploadFileAsync(imageBytes, fileName, mimeType, "Indigo image staged upload");
            var shopifyFile = fileCreateResponse.Files.Count > 0 ? fileCreateResponse.Files[0] : null;
            if (shopifyFile != null)
            {
                Console.WriteLine($"\n🎉 Shopify file created!");
                Console.WriteLine($"File ID: {shopifyFile.Id}");
                Console.WriteLine($"Status: {shopifyFile.FileStatus}");
                if (shopifyFile.Image != null && !string.IsNullOrEmpty(shopifyFile.Image.Url))
                {
                    Console.WriteLine($"Shopify CDN URL: {shopifyFile.Image.Url}");
                }
                else
                {
                    Console.WriteLine($"No CDN image URL returned.");
                }
                Console.WriteLine($"Check your Shopify dashboard under Content > Files.");
            }
            else
            {
                Console.WriteLine($"❌ No file returned from Shopify.");
            }
        }
    }
} 