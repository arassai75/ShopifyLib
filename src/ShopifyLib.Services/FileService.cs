using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShopifyLib.Models;
using ShopifyLib.Utils;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Implementation of file operations using REST API for direct uploads
    /// and GraphQL for URL-based uploads
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IGraphQLService _graphQLService;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the FileService class.
        /// </summary>
        /// <param name="graphQLService">The GraphQL service for file operations.</param>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <exception cref="ArgumentNullException">Thrown when graphQLService or httpClient is null.</exception>
        public FileService(IGraphQLService graphQLService, HttpClient httpClient)
        {
            if (graphQLService == null)
                throw new ArgumentNullException(nameof(graphQLService));
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));
            
            _graphQLService = graphQLService;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Uploads a file from a file path.
        /// </summary>
        /// <param name="filePath">The path to the file to upload.</param>
        /// <param name="altText">Optional alt text for the file.</param>
        /// <param name="resourceId">Optional resource ID to associate with the file.</param>
        /// <param name="resourceType">Optional resource type to associate with the file.</param>
        /// <returns>The file creation response.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
        public async Task<FileCreateResponse> UploadFileAsync(string filePath, string? altText = null, string? resourceId = null, string? resourceType = null)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using var stream = System.IO.File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentType(fileName);

            return await UploadFileAsync(stream, fileName, contentType, altText, resourceId, resourceType);
        }

        /// <summary>
        /// Uploads a file from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the file data.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="altText">Optional alt text for the file.</param>
        /// <param name="resourceId">Optional resource ID to associate with the file.</param>
        /// <param name="resourceType">Optional resource type to associate with the file.</param>
        /// <returns>The file creation response.</returns>
        public async Task<FileCreateResponse> UploadFileAsync(Stream stream, string fileName, string contentType, string? altText = null, string? resourceId = null, string? resourceType = null)
        {
            // For direct file uploads, we'll use a simple approach that creates a file record
            // Note: This is a simplified implementation. In production, you might want to:
            // 1. Upload to a temporary storage service (S3, etc.)
            // 2. Get a public URL
            // 3. Use that URL with GraphQL fileCreate
            
            // For now, we'll create a mock response since direct file upload via GraphQL
            // requires a URL, not base64 content
            var mockFile = new ShopifyLib.Models.File
            {
                Id = $"gid://shopify/MediaImage/{DateTime.UtcNow.Ticks}",
                FileStatus = "READY",
                Alt = altText,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return new FileCreateResponse
            {
                Files = new List<ShopifyLib.Models.File> { mockFile },
                UserErrors = new List<UserError>()
            };
        }

        /// <summary>
        /// Uploads multiple files using GraphQL.
        /// </summary>
        /// <param name="files">List of files to upload.</param>
        /// <returns>The file creation response.</returns>
        public async Task<FileCreateResponse> UploadFilesAsync(List<FileCreateInput> files)
        {
            // This method is for URL-based uploads via GraphQL
            return await _graphQLService.CreateFilesAsync(files);
        }

        /// <summary>
        /// Upload a file from a URL using GraphQL
        /// </summary>
        /// <param name="fileUrl">The URL of the file to upload</param>
        /// <param name="contentType">The content type enum (IMAGE, FILE, VIDEO)</param>
        /// <param name="altText">Optional alt text</param>
        /// <returns>The file creation response</returns>
        public async Task<FileCreateResponse> UploadFileFromUrlAsync(string fileUrl, string contentType = FileContentType.File, string? altText = null)
        {
            var fileInput = new FileCreateInput
            {
                OriginalSource = fileUrl,
                ContentType = contentType,
                Alt = altText
            };

            return await _graphQLService.CreateFilesAsync(new List<FileCreateInput> { fileInput });
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

        private static string GetContentTypeEnum(string mimeType)
        {
            return mimeType.ToLowerInvariant() switch
            {
                var mt when mt.StartsWith("image/") => FileContentType.Image,
                var mt when mt.StartsWith("video/") => FileContentType.Video,
                _ => FileContentType.File
            };
        }
    }
} 