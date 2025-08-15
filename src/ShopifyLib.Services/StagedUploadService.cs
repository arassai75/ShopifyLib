using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Service for handling staged file uploads to Shopify
    /// This approach can bypass URL download issues by uploading files directly
    /// </summary>
    public class StagedUploadService
    {
        private readonly IGraphQLService _graphQLService;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the StagedUploadService class.
        /// </summary>
        /// <param name="graphQLService">The GraphQL service for staged upload operations.</param>
        /// <param name="httpClient">The HTTP client for making upload requests.</param>
        /// <exception cref="ArgumentNullException">Thrown when graphQLService or httpClient is null.</exception>
        public StagedUploadService(IGraphQLService graphQLService, HttpClient httpClient)
        {
            if (graphQLService == null)
                throw new ArgumentNullException(nameof(graphQLService));
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));
            
            _graphQLService = graphQLService;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Uploads a file using the staged upload approach
        /// </summary>
        /// <param name="fileBytes">The file bytes to upload</param>
        /// <param name="fileName">The filename</param>
        /// <param name="contentType">The MIME type</param>
        /// <param name="altText">Optional alt text for the file</param>
        /// <returns>The file creation response</returns>
        public async Task<FileCreateResponse> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string? altText = null)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("File bytes cannot be null or empty", nameof(fileBytes));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));

            // Step 1: Create staged upload
            var stagedInput = new StagedUploadInput
            {
                Filename = fileName,
                MimeType = contentType,
                Resource = GetResourceType(contentType),
                FileSize = fileBytes.Length.ToString()
            };

            var stagedResponse = await _graphQLService.CreateStagedUploadAsync(stagedInput);
            
            if (stagedResponse.StagedTarget == null)
            {
                throw new InvalidOperationException("Failed to create staged upload - no staged target returned");
            }

            // Step 2: Upload file to staged URL
            await UploadToStagedUrlAsync(stagedResponse.StagedTarget, fileBytes, fileName, contentType);

            // Step 3: Create file using the staged resource URL
            var fileInput = new FileCreateInput
            {
                OriginalSource = stagedResponse.StagedTarget.ResourceUrl,
                ContentType = GetContentTypeEnum(contentType),
                Alt = altText
            };

            return await _graphQLService.CreateFilesAsync(new List<FileCreateInput> { fileInput });
        }

        /// <summary>
        /// Uploads a file from a stream using the staged upload approach
        /// </summary>
        /// <param name="stream">The file stream</param>
        /// <param name="fileName">The filename</param>
        /// <param name="contentType">The MIME type</param>
        /// <param name="altText">Optional alt text for the file</param>
        /// <returns>The file creation response</returns>
        public async Task<FileCreateResponse> UploadFileAsync(Stream stream, string fileName, string contentType, string? altText = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));

            // Read stream into byte array
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            return await UploadFileAsync(fileBytes, fileName, contentType, altText);
        }

        /// <summary>
        /// Downloads a file from a URL and uploads it using staged upload
        /// This can bypass URL download issues that occur with direct fileCreate
        /// </summary>
        /// <param name="url">The URL to download the file from</param>
        /// <param name="fileName">The filename to use</param>
        /// <param name="contentType">The MIME type</param>
        /// <param name="altText">Optional alt text for the file</param>
        /// <param name="userAgent">Optional User-Agent header for the download</param>
        /// <returns>The file creation response</returns>
        public async Task<FileCreateResponse> UploadFileFromUrlAsync(string url, string fileName, string contentType, string? altText = null, string? userAgent = null)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));

            // Download the file
            var fileBytes = await DownloadFileAsync(url, userAgent);

            return await UploadFileAsync(fileBytes, fileName, contentType, altText);
        }

        /// <summary>
        /// Uploads multiple files using staged upload
        /// </summary>
        /// <param name="files">List of file data to upload</param>
        /// <returns>The file creation response</returns>
        public async Task<FileCreateResponse> UploadFilesAsync(List<(byte[] Bytes, string FileName, string ContentType, string? AltText)> files)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("Files list cannot be null or empty", nameof(files));

            var fileInputs = new List<FileCreateInput>();

            foreach (var (bytes, fileName, contentType, altText) in files)
            {
                // Create staged upload for each file
                var stagedInput = new StagedUploadInput
                {
                    Filename = fileName,
                    MimeType = contentType,
                    Resource = GetResourceType(contentType),
                    FileSize = bytes.Length.ToString()
                };

                var stagedResponse = await _graphQLService.CreateStagedUploadAsync(stagedInput);
                
                if (stagedResponse.StagedTarget == null)
                {
                    throw new InvalidOperationException($"Failed to create staged upload for {fileName}");
                }

                // Upload file to staged URL
                await UploadToStagedUrlAsync(stagedResponse.StagedTarget, bytes, fileName, contentType);

                // Add to file inputs
                fileInputs.Add(new FileCreateInput
                {
                    OriginalSource = stagedResponse.StagedTarget.ResourceUrl,
                    ContentType = GetContentTypeEnum(contentType),
                    Alt = altText
                });
            }

            // Create all files at once
            return await _graphQLService.CreateFilesAsync(fileInputs);
        }

        /// <summary>
        /// Uploads a file to the staged URL
        /// </summary>
        /// <param name="stagedTarget">The staged upload target</param>
        /// <param name="fileBytes">The file bytes</param>
        /// <param name="fileName">The filename</param>
        /// <param name="contentType">The content type</param>
        private async Task UploadToStagedUrlAsync(StagedUploadTarget stagedTarget, byte[] fileBytes, string fileName, string contentType)
        {
            // Debug: Log the staged target parameters in exact order
            Console.WriteLine($"🔍 Debug: Staged upload parameters (exact order for multipart form):");
            Console.WriteLine($"   URL: {stagedTarget.Url}");
            Console.WriteLine($"   Boundary: ----WebKitFormBoundary{DateTime.Now.Ticks:X}");
            Console.WriteLine();
            Console.WriteLine("   PARAMETERS (in order):");
            for (int i = 0; i < stagedTarget.Parameters.Count; i++)
            {
                var parameter = stagedTarget.Parameters[i];
                Console.WriteLine($"   [{i + 1}] {parameter.Name} = {parameter.Value}");
            }
            Console.WriteLine($"   [File] file = {fileName} (Content-Type: {contentType}, Size: {fileBytes.Length} bytes)");
            Console.WriteLine();

            // Generate a unique boundary for the multipart form - use a simpler format
            var boundary = $"----WebKitFormBoundary{Guid.NewGuid().ToString("N").Substring(0, 16)}";
            
            // Build the multipart form data manually to match exactly what Shopify expects
            using var ms = new MemoryStream();
            
            // Write headers directly as bytes to avoid BOM
            var utf8NoBom = new System.Text.UTF8Encoding(false);
            
            // Add all parameters first, in order
            foreach (var parameter in stagedTarget.Parameters)
            {
                var header = utf8NoBom.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"{parameter.Name}\"\r\n\r\n{parameter.Value}\r\n");
                ms.Write(header, 0, header.Length);
            }

            // Add the file last
            var fileHeader = utf8NoBom.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\nContent-Type: {contentType}\r\n\r\n");
            ms.Write(fileHeader, 0, fileHeader.Length);

            // Write the file bytes directly after the headers
            ms.Write(fileBytes, 0, fileBytes.Length);

            // Write the closing boundary immediately after the file bytes
            var closing = utf8NoBom.GetBytes($"\r\n--{boundary}--\r\n");
            ms.Write(closing, 0, closing.Length);

            var formData = ms.ToArray();

            // Debug: Print the exact multipart form structure
            Console.WriteLine("==== MULTIPART FORM STRUCTURE ====");
            Console.WriteLine($"Boundary: {boundary}");
            Console.WriteLine();
            
            // Show the exact structure that will be generated
            using var debugMs = new MemoryStream();
            using (var debugWriter = new StreamWriter(debugMs, System.Text.Encoding.UTF8, 1024, leaveOpen: true))
            {
                foreach (var parameter in stagedTarget.Parameters)
                {
                    debugWriter.Write($"--{boundary}\r\n");
                    debugWriter.Write($"Content-Disposition: form-data; name=\"{parameter.Name}\"\r\n");
                    debugWriter.Write($"\r\n");
                    debugWriter.Write($"{parameter.Value}\r\n");
                }

                debugWriter.Write($"--{boundary}\r\n");
                debugWriter.Write($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n");
                debugWriter.Write($"Content-Type: {contentType}\r\n");
                debugWriter.Write($"\r\n");
                debugWriter.Flush();
            }
            
            var debugFormData = debugMs.ToArray();
            var debugPreviewLength = Math.Min(debugFormData.Length, 1024);
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(debugFormData, 0, debugPreviewLength));
            Console.WriteLine($"[... {fileBytes.Length} bytes of file data ...]");
            Console.WriteLine($"\r\n--{boundary}--\r\n");
            Console.WriteLine("==== END MULTIPART FORM STRUCTURE ====");
            Console.WriteLine();

            // Debug: Print the first 1024 bytes of the raw multipart body
            Console.WriteLine("==== BEGIN MULTIPART BODY (first 1024 bytes) ====");
            var previewLength = Math.Min(formData.Length, 1024);
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(formData, 0, previewLength));
            Console.WriteLine("==== END MULTIPART BODY PREVIEW ====");
            
            // Debug: Show hex dump of first 256 bytes to see exact byte values
            Console.WriteLine("==== HEX DUMP (first 256 bytes) ====");
            var hexLength = Math.Min(formData.Length, 256);
            for (int i = 0; i < hexLength; i += 16)
            {
                var lineBytes = Math.Min(16, hexLength - i);
                var hex = string.Join(" ", formData.Skip(i).Take(lineBytes).Select(b => b.ToString("X2")));
                var ascii = string.Join("", formData.Skip(i).Take(lineBytes).Select(b => b >= 32 && b <= 126 ? (char)b : '.'));
                Console.WriteLine($"{i:X4}: {hex,-48} {ascii}");
            }
            Console.WriteLine("==== END HEX DUMP ====");

            // Create the request manually to avoid any automatic header modifications
            using var request = new HttpRequestMessage(HttpMethod.Put, stagedTarget.Url);
            request.Content = new ByteArrayContent(formData);
            request.Content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

            // Debug: Show the request details
            Console.WriteLine($"   [DEBUG] Request URL: {stagedTarget.Url}");
            Console.WriteLine($"   [DEBUG] Content-Type: {request.Content.Headers.ContentType}");
            Console.WriteLine($"   [DEBUG] Content-Length: {formData.Length}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var respBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload to staged URL: {response.StatusCode} - {respBody}");
            }
        }

        /// <summary>
        /// Minimal, robust multipart upload for Shopify staged upload (for debugging)
        /// </summary>
        /// <param name="url">The staged upload URL</param>
        /// <param name="contentType">The MIME type</param>
        /// <param name="fileName">The file name</param>
        /// <param name="fileBytes">The file bytes</param>
        /// <param name="acl">ACL (default: private)</param>
        public static async Task UploadToShopifyStagedUrlMinimal(
            string url,
            string contentType,
            string fileName,
            byte[] fileBytes,
            string acl = "private")
        {
            var boundary = "----ShopifyBoundary" + Guid.NewGuid().ToString("N").Substring(0, 16);
            var utf8NoBom = new System.Text.UTF8Encoding(false);

            using var ms = new MemoryStream();

            // content_type
            var contentTypeBytes = utf8NoBom.GetBytes($"--{boundary}\r\n");
            ms.Write(contentTypeBytes, 0, contentTypeBytes.Length);
            var contentTypeHeaderBytes = utf8NoBom.GetBytes("Content-Disposition: form-data; name=\"content_type\"\r\n\r\n");
            ms.Write(contentTypeHeaderBytes, 0, contentTypeHeaderBytes.Length);
            var contentTypeValueBytes = utf8NoBom.GetBytes($"{contentType}\r\n");
            ms.Write(contentTypeValueBytes, 0, contentTypeValueBytes.Length);

            // acl
            var aclBoundaryBytes = utf8NoBom.GetBytes($"--{boundary}\r\n");
            ms.Write(aclBoundaryBytes, 0, aclBoundaryBytes.Length);
            var aclHeaderBytes = utf8NoBom.GetBytes("Content-Disposition: form-data; name=\"acl\"\r\n\r\n");
            ms.Write(aclHeaderBytes, 0, aclHeaderBytes.Length);
            var aclValueBytes = utf8NoBom.GetBytes($"{acl}\r\n");
            ms.Write(aclValueBytes, 0, aclValueBytes.Length);

            // file
            var fileBoundaryBytes = utf8NoBom.GetBytes($"--{boundary}\r\n");
            ms.Write(fileBoundaryBytes, 0, fileBoundaryBytes.Length);
            var fileHeaderBytes = utf8NoBom.GetBytes($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n");
            ms.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
            var fileContentTypeBytes = utf8NoBom.GetBytes($"Content-Type: {contentType}\r\n\r\n");
            ms.Write(fileContentTypeBytes, 0, fileContentTypeBytes.Length);
            ms.Write(fileBytes, 0, fileBytes.Length);
            var closingBytes = utf8NoBom.GetBytes($"\r\n--{boundary}--\r\n");
            ms.Write(closingBytes, 0, closingBytes.Length);

            ms.Seek(0, SeekOrigin.Begin);

            using var client = new HttpClient();
            using var content = new StreamContent(ms);
            content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

            var response = await client.PutAsync(url, content);
            var respBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Upload failed: {response.StatusCode}\n{respBody}");
            }
            else
            {
                Console.WriteLine("Upload succeeded!");
                Console.WriteLine(respBody);
            }
        }

        /// <summary>
        /// Downloads a file from a URL
        /// </summary>
        /// <param name="url">The URL to download from</param>
        /// <param name="userAgent">Optional User-Agent header</param>
        /// <returns>The file bytes</returns>
        private async Task<byte[]> DownloadFileAsync(string url, string? userAgent = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.Headers.UserAgent.ParseAdd(userAgent);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to download file from {url}: {response.StatusCode}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }

        /// <summary>
        /// Gets the resource type based on content type
        /// </summary>
        /// <param name="contentType">The MIME type</param>
        /// <returns>The resource type</returns>
        private static string GetResourceType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                var ct when ct.StartsWith("image/") => "IMAGE",
                var ct when ct.StartsWith("video/") => "VIDEO",
                _ => "FILE"
            };
        }

        /// <summary>
        /// Gets the content type enum based on MIME type
        /// </summary>
        /// <param name="contentType">The MIME type</param>
        /// <returns>The content type enum</returns>
        private static string GetContentTypeEnum(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                var ct when ct.StartsWith("image/") => FileContentType.Image,
                var ct when ct.StartsWith("video/") => FileContentType.Video,
                _ => FileContentType.File
            };
        }
    }
} 