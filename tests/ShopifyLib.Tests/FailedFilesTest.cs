using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using ShopifyLib;
using ShopifyLib.Configuration;
using ShopifyLib.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ShopifyLib.Tests
{
    [IntegrationTest]
    public class FailedFilesTest : IDisposable
    {
        private readonly ShopifyClient _client;

        public FailedFilesTest()
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
        public async Task GetFailedFiles_ListErrors()
        {
            Console.WriteLine("=== FAILED FILES ANALYSIS ===");
            Console.WriteLine("This test retrieves all failed files from Shopify and shows their error details");
            Console.WriteLine();

            // GraphQL query to get files with failed status
            var query = @"
                query GetFailedFiles($first: Int!, $after: String) {
                    files(first: $first, after: $after, query: ""status:FAILED"") {
                        edges {
                            node {
                                id
                                fileStatus
                                createdAt
                                updatedAt
                                ... on MediaImage {
                                    alt
                                    mediaContentType
                                    preview {
                                        image {
                                            url
                                            width
                                            height
                                        }
                                    }
                                    image {
                                        url
                                        width
                                        height
                                    }
                                }
                                ... on GenericFile {
                                    originalFileSize
                                    mimeType
                                    preview {
                                        image {
                                            url
                                            width
                                            height
                                        }
                                    }
                                }
                            }
                        }
                        pageInfo {
                            hasNextPage
                            endCursor
                        }
                    }
                }";

            var variables = new
            {
                first = 50, // Get up to 50 failed files
                after = (string)null
            };

            try
            {
                var response = await _client.GraphQL.ExecuteQueryAsync(query, variables);
                
                // Parse the JSON response
                var responseData = JsonConvert.DeserializeObject<dynamic>(response);
                
                if (responseData.errors != null)
                {
                    Console.WriteLine("❌ GraphQL Errors:");
                    foreach (var error in responseData.errors)
                    {
                        Console.WriteLine($"   - {error.message}");
                    }
                    return;
                }

                var filesData = responseData.data?.files;
                if (filesData == null)
                {
                    Console.WriteLine("❌ No files data returned");
                    return;
                }

                var failedFiles = filesData.edges;
                if (failedFiles == null || failedFiles.Count == 0)
                {
                    Console.WriteLine("✅ No failed files found!");
                    return;
                }

                Console.WriteLine($"📊 Found {failedFiles.Count} failed files:");
                Console.WriteLine();

                for (int i = 0; i < failedFiles.Count; i++)
                {
                    var file = failedFiles[i].node;
                    Console.WriteLine($"--- Failed File #{i + 1} ---");
                    Console.WriteLine($"🆔 File ID: {file.id}");
                    Console.WriteLine($"📊 Status: {file.fileStatus}");
                    Console.WriteLine($"📅 Created: {file.createdAt}");
                    Console.WriteLine($"🔄 Updated: {file.updatedAt}");
                    
                    if (file.originalFileSize != null)
                    {
                        Console.WriteLine($"📏 File Size: {file.originalFileSize} bytes");
                    }

                    if (file.alt != null)
                    {
                        Console.WriteLine($"🏷️ Alt Text: {file.alt}");
                    }

                    if (file.mediaContentType != null)
                    {
                        Console.WriteLine($"🎬 Media Type: {file.mediaContentType}");
                    }

                    if (file.mimeType != null)
                    {
                        Console.WriteLine($"📄 MIME Type: {file.mimeType}");
                    }

                    if (file.image != null)
                    {
                        Console.WriteLine($"🖼️ Image URL: {file.image.url ?? "Not available"}");
                        if (file.image.width != null && file.image.height != null)
                        {
                            Console.WriteLine($"📐 Dimensions: {file.image.width}x{file.image.height}");
                        }
                    }

                    if (file.preview?.image != null)
                    {
                        Console.WriteLine($"👁️ Preview URL: {file.preview.image.url ?? "Not available"}");
                    }

                    Console.WriteLine();
                }

                // Also try to get more detailed error information
                Console.WriteLine("=== DETAILED ERROR ANALYSIS ===");
                await GetDetailedFileErrors(failedFiles);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving failed files: {ex.Message}");
            }
        }

        private async Task GetDetailedFileErrors(dynamic failedFiles)
        {
            // Try to get more detailed error information for each failed file
            foreach (var fileEdge in failedFiles)
            {
                var file = fileEdge.node;
                var fileId = file.id.ToString();
                
                Console.WriteLine($"🔍 Analyzing file: {fileId}");
                
                // Try to get file details with potential error information
                var detailQuery = @"
                    query GetFileDetails($id: ID!) {
                        node(id: $id) {
                            ... on MediaImage {
                                id
                                fileStatus
                                preview {
                                    image {
                                        url
                                    }
                                }
                                image {
                                    url
                                }
                            }
                            ... on GenericFile {
                                id
                                fileStatus
                                preview {
                                    image {
                                        url
                                    }
                                }
                            }
                        }
                    }";

                try
                {
                    var detailResponse = await _client.GraphQL.ExecuteQueryAsync(detailQuery, new { id = fileId });
                    var detailResponseData = JsonConvert.DeserializeObject<dynamic>(detailResponse);
                    var node = detailResponseData.data?.node;
                    if (node != null)
                    {
                        Console.WriteLine($"   📊 Current Status: {node.fileStatus}");
                        // Check for image.url
                        if (node.image != null && node.image.Type == Newtonsoft.Json.Linq.JTokenType.Object && node.image.url != null)
                        {
                            Console.WriteLine($"   ✅ Image URL: {node.image.url}");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ No image URL available");
                        }
                        // Check for preview.image.url
                        if (node.preview != null && node.preview.Type == Newtonsoft.Json.Linq.JTokenType.Object && node.preview.image != null && node.preview.image.Type == Newtonsoft.Json.Linq.JTokenType.Object && node.preview.image.url != null)
                        {
                            Console.WriteLine($"   ✅ Preview URL: {node.preview.image.url}");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ No preview URL available");
                        }
                    }
                    else if (detailResponseData.errors != null)
                    {
                        Console.WriteLine($"   ❌ Errors for this file:");
                        foreach (var error in detailResponseData.errors)
                        {
                            Console.WriteLine($"      - {error.message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error getting details: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        [Fact]
        public async Task GetAllFiles_CheckStatuses()
        {
            Console.WriteLine("=== ALL FILES STATUS CHECK ===");
            Console.WriteLine("This test retrieves all files and categorizes them by status");
            Console.WriteLine();

            var query = @"
                query GetAllFiles($first: Int!, $after: String) {
                    files(first: $first, after: $after) {
                        edges {
                            node {
                                id
                                fileStatus
                                createdAt
                                ... on MediaImage {
                                    alt
                                }
                                ... on GenericFile {
                                    originalFileSize
                                    mimeType
                                }
                            }
                        }
                        pageInfo {
                            hasNextPage
                            endCursor
                        }
                    }
                }";

            var variables = new
            {
                first = 100,
                after = (string)null
            };

            try
            {
                var response = await _client.GraphQL.ExecuteQueryAsync(query, variables);
                
                // Parse the JSON response
                var responseData = JsonConvert.DeserializeObject<dynamic>(response);
                
                if (responseData.errors != null)
                {
                    Console.WriteLine("❌ GraphQL Errors:");
                    foreach (var error in responseData.errors)
                    {
                        Console.WriteLine($"   - {error.message}");
                    }
                    return;
                }

                var filesData = responseData.data?.files;
                if (filesData == null)
                {
                    Console.WriteLine("❌ No files data returned");
                    return;
                }

                var files = filesData.edges;
                if (files == null || files.Count == 0)
                {
                    Console.WriteLine("✅ No files found!");
                    return;
                }

                // Categorize files by status
                var statusCounts = new Dictionary<string, int>();
                var failedFiles = new List<dynamic>();
                var uploadedFiles = new List<dynamic>();
                var processingFiles = new List<dynamic>();

                foreach (var fileEdge in files)
                {
                    var file = fileEdge.node;
                    var status = file.fileStatus.ToString();
                    
                    if (!statusCounts.ContainsKey(status))
                        statusCounts[status] = 0;
                    statusCounts[status]++;

                    switch (status.ToUpper())
                    {
                        case "FAILED":
                            failedFiles.Add(file);
                            break;
                        case "UPLOADED":
                            uploadedFiles.Add(file);
                            break;
                        case "PROCESSING":
                            processingFiles.Add(file);
                            break;
                    }
                }

                Console.WriteLine("📊 File Status Summary:");
                foreach (var kvp in statusCounts.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value} files");
                }
                Console.WriteLine();

                if (failedFiles.Count > 0)
                {
                    Console.WriteLine($"❌ Failed Files ({failedFiles.Count}):");
                    foreach (var file in failedFiles.Take(10)) // Show first 10
                    {
                        Console.WriteLine($"   - {file.id} (Created: {file.createdAt})");
                    }
                    if (failedFiles.Count > 10)
                    {
                        Console.WriteLine($"   ... and {failedFiles.Count - 10} more");
                    }
                    Console.WriteLine();
                }

                if (processingFiles.Count > 0)
                {
                    Console.WriteLine($"⏳ Processing Files ({processingFiles.Count}):");
                    foreach (var file in processingFiles.Take(5)) // Show first 5
                    {
                        Console.WriteLine($"   - {file.id} (Created: {file.createdAt})");
                    }
                    if (processingFiles.Count > 5)
                    {
                        Console.WriteLine($"   ... and {processingFiles.Count - 5} more");
                    }
                    Console.WriteLine();
                }

                if (uploadedFiles.Count > 0)
                {
                    Console.WriteLine($"✅ Successfully Uploaded Files ({uploadedFiles.Count}):");
                    foreach (var file in uploadedFiles.Take(5)) // Show first 5
                    {
                        Console.WriteLine($"   - {file.id} (Created: {file.createdAt})");
                    }
                    if (uploadedFiles.Count > 5)
                    {
                        Console.WriteLine($"   ... and {uploadedFiles.Count - 5} more");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving files: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
} 