using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Newtonsoft.Json;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class SimpleGraphQLTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public SimpleGraphQLTest()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var shopifyConfig = configuration.GetShopifyConfig();
            
            if (!shopifyConfig.IsValid())
            {
                throw new InvalidOperationException("Shopify configuration is not valid. Please check your appsettings.json or environment variables.");
            }

            _client = new ShopifyClient(shopifyConfig);
        }

        [Fact]
        public async Task SimpleFileUpload_CheckAvailableFields()
        {
            // Arrange - Use the specified Indigo image URL
            var imageUrl = "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85";
            var altText = "Indigo Gift Image - Simple Test";
            
            Console.WriteLine("=== SIMPLE GRAPHQL FILE UPLOAD TEST ===");
            Console.WriteLine("This test uses a simple GraphQL mutation to see what fields are available");
            Console.WriteLine($"Image URL: {imageUrl}");
            Console.WriteLine($"Alt Text: {altText}");
            Console.WriteLine();

            try
            {
                // Use a simple GraphQL mutation to see what's available
                Console.WriteLine("🔄 Testing simple GraphQL mutation...");
                
                var simpleMutation = @"
                    mutation fileCreate($files: [FileCreateInput!]!) {
                        fileCreate(files: $files) {
                            files {
                                id
                                fileStatus
                                alt
                                createdAt
                            }
                            userErrors {
                                field
                                message
                            }
                        }
                    }";

                var fileInput = new FileCreateInput
                {
                    OriginalSource = imageUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                };

                var variables = new { files = new List<FileCreateInput> { fileInput } };
                var rawResponse = await _client.GraphQL.ExecuteQueryAsync(simpleMutation, variables);

                Console.WriteLine("✅ Simple GraphQL mutation executed!");
                Console.WriteLine();
                Console.WriteLine("=== RAW GRAPHQL RESPONSE ===");
                Console.WriteLine(rawResponse);
                Console.WriteLine();

                // Try to parse the response
                try
                {
                    var parsedResponse = JsonConvert.DeserializeObject<dynamic>(rawResponse);
                    Console.WriteLine("=== PARSED RESPONSE ===");
                    Console.WriteLine(JsonConvert.SerializeObject(parsedResponse, Formatting.Indented));
                    Console.WriteLine();

                    // Check if there are any files in the response
                    if (parsedResponse?.data?.fileCreate?.files != null)
                    {
                        var files = parsedResponse.data.fileCreate.files;
                        Console.WriteLine($"📋 Found {files.Count} files in response");
                        
                        foreach (var file in files)
                        {
                            Console.WriteLine();
                            Console.WriteLine("=== FILE DETAILS ===");
                            Console.WriteLine($"📁 ID: {file.id}");
                            Console.WriteLine($"📊 Status: {file.fileStatus}");
                            Console.WriteLine($"📝 Alt: {file.alt}");
                            Console.WriteLine($"📅 Created: {file.createdAt}");
                            
                            // Check what other fields might be available
                            Console.WriteLine("🔍 Available fields:");
                            foreach (var property in file)
                            {
                                Console.WriteLine($"   • {property.Name}: {property.Value}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No files found in response");
                    }

                    // Check for user errors
                    if (parsedResponse?.data?.fileCreate?.userErrors != null)
                    {
                        var errors = parsedResponse.data.fileCreate.userErrors;
                        Console.WriteLine();
                        Console.WriteLine("⚠️  USER ERRORS:");
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"   • {error.message}");
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"❌ Failed to parse response: {parseEx.Message}");
                }

                // Now try the enhanced mutation
                Console.WriteLine();
                Console.WriteLine("=== TESTING ENHANCED MUTATION ===");
                
                var enhancedMutation = @"
                    mutation fileCreate($files: [FileCreateInput!]!) {
                        fileCreate(files: $files) {
                            files {
                                id
                                fileStatus
                                alt
                                createdAt
                                ... on MediaImage {
                                    image {
                                        width
                                        height
                                        url
                                        originalSrc
                                        transformedSrc
                                        src
                                    }
                                }
                            }
                            userErrors {
                                field
                                message
                            }
                        }
                    }";

                var enhancedResponse = await _client.GraphQL.ExecuteQueryAsync(enhancedMutation, variables);

                Console.WriteLine("✅ Enhanced GraphQL mutation executed!");
                Console.WriteLine();
                Console.WriteLine("=== ENHANCED RAW RESPONSE ===");
                Console.WriteLine(enhancedResponse);
                Console.WriteLine();

                // Parse enhanced response
                try
                {
                    var enhancedParsed = JsonConvert.DeserializeObject<dynamic>(enhancedResponse);
                    Console.WriteLine("=== ENHANCED PARSED RESPONSE ===");
                    Console.WriteLine(JsonConvert.SerializeObject(enhancedParsed, Formatting.Indented));
                    Console.WriteLine();

                    if (enhancedParsed?.data?.fileCreate?.files != null)
                    {
                        var files = enhancedParsed.data.fileCreate.files;
                        Console.WriteLine($"📋 Found {files.Count} files in enhanced response");
                        
                        foreach (var file in files)
                        {
                            Console.WriteLine();
                            Console.WriteLine("=== ENHANCED FILE DETAILS ===");
                            Console.WriteLine($"📁 ID: {file.id}");
                            Console.WriteLine($"📊 Status: {file.fileStatus}");
                            Console.WriteLine($"📝 Alt: {file.alt}");
                            Console.WriteLine($"📅 Created: {file.createdAt}");
                            
                            if (file.image != null)
                            {
                                Console.WriteLine("📸 IMAGE DETAILS:");
                                Console.WriteLine($"   • Width: {file.image.width}");
                                Console.WriteLine($"   • Height: {file.image.height}");
                                Console.WriteLine($"   • URL: {file.image.url}");
                                Console.WriteLine($"   • OriginalSrc: {file.image.originalSrc}");
                                Console.WriteLine($"   • TransformedSrc: {file.image.transformedSrc}");
                                Console.WriteLine($"   • Src: {file.image.src}");
                            }
                            else
                            {
                                Console.WriteLine("❌ No image details available");
                            }
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"❌ Failed to parse enhanced response: {parseEx.Message}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 