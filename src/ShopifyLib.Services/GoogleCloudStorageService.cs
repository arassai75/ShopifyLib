using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Linq; // Added for .AllKeys and .Any
using ShopifyLib.Services.Interfaces;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Service for handling Google Cloud Storage signed URL uploads
    /// </summary>
    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly HttpClient _httpClient;

        public GoogleCloudStorageService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Uploads a file to a Google Cloud Storage signed URL
        /// </summary>
        /// <param name="signedUrl">The signed URL from Google Cloud Storage</param>
        /// <param name="fileBytes">The file bytes to upload</param>
        /// <param name="contentType">The MIME type of the file</param>
        /// <param name="fileName">The filename</param>
        /// <returns>Upload response</returns>
        public async Task<HttpResponseMessage> UploadToSignedUrlAsync(string signedUrl, byte[] fileBytes, string contentType, string fileName)
        {
            if (string.IsNullOrEmpty(signedUrl))
                throw new ArgumentException("Signed URL cannot be null or empty", nameof(signedUrl));
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("File bytes cannot be null or empty", nameof(fileBytes));
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));

            // Parse the signed URL to extract query parameters
            var uri = new Uri(signedUrl);
            var queryParams = ParseQueryString(uri.Query);

            // Extract required parameters from the signed URL
            queryParams.TryGetValue("X-Goog-Algorithm", out var xGoogAlgorithm);
            queryParams.TryGetValue("X-Goog-Credential", out var xGoogCredential);
            queryParams.TryGetValue("X-Goog-Date", out var xGoogDate);
            queryParams.TryGetValue("X-Goog-Expires", out var xGoogExpires);
            queryParams.TryGetValue("X-Goog-SignedHeaders", out var xGoogSignedHeaders);
            queryParams.TryGetValue("X-Goog-Signature", out var xGoogSignature);

            // Create the request
            using var request = new HttpRequestMessage(HttpMethod.Put, signedUrl);
            
            // Set content
            request.Content = new ByteArrayContent(fileBytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            // Add required headers for signed URL
            if (!string.IsNullOrEmpty(xGoogAlgorithm))
                request.Headers.Add("X-Goog-Algorithm", xGoogAlgorithm);
            if (!string.IsNullOrEmpty(xGoogCredential))
                request.Headers.Add("X-Goog-Credential", xGoogCredential);
            if (!string.IsNullOrEmpty(xGoogDate))
                request.Headers.Add("X-Goog-Date", xGoogDate);
            if (!string.IsNullOrEmpty(xGoogExpires))
                request.Headers.Add("X-Goog-Expires", xGoogExpires);
            if (!string.IsNullOrEmpty(xGoogSignedHeaders))
                request.Headers.Add("X-Goog-SignedHeaders", xGoogSignedHeaders);
            if (!string.IsNullOrEmpty(xGoogSignature))
                request.Headers.Add("X-Goog-Signature", xGoogSignature);

            // Add content length header
            request.Headers.Add("Content-Length", fileBytes.Length.ToString());

            // Send the request
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Uploads a file from stream to a Google Cloud Storage signed URL
        /// </summary>
        /// <param name="signedUrl">The signed URL from Google Cloud Storage</param>
        /// <param name="stream">The file stream</param>
        /// <param name="contentType">The MIME type of the file</param>
        /// <param name="fileName">The filename</param>
        /// <returns>Upload response</returns>
        public async Task<HttpResponseMessage> UploadToSignedUrlAsync(string signedUrl, Stream stream, string contentType, string fileName)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Read stream into byte array
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            return await UploadToSignedUrlAsync(signedUrl, fileBytes, contentType, fileName);
        }

        /// <summary>
        /// Generates a signed URL for Google Cloud Storage (for testing purposes)
        /// Note: In production, this should be done server-side with proper credentials
        /// </summary>
        /// <param name="bucketName">The GCS bucket name</param>
        /// <param name="objectName">The object name in the bucket</param>
        /// <param name="expirationMinutes">Expiration time in minutes</param>
        /// <param name="serviceAccountKey">The service account key JSON</param>
        /// <returns>A signed URL</returns>
        public static string GenerateSignedUrl(string bucketName, string objectName, int expirationMinutes, string serviceAccountKey)
        {
            // This is a simplified example. In production, you should use Google Cloud SDK
            // or implement proper HMAC-SHA256 signing with the service account credentials
            
            var expirationTime = DateTime.UtcNow.AddMinutes(expirationMinutes);
            var expirationTimestamp = ((DateTimeOffset)expirationTime).ToUnixTimeSeconds();
            
            // This is a placeholder - actual implementation requires proper GCS signing
            var baseUrl = $"https://storage.googleapis.com/{bucketName}/{objectName}";
            var queryParams = new Dictionary<string, string>
            {
                ["X-Goog-Algorithm"] = "GOOG4-RSA-SHA256",
                ["X-Goog-Credential"] = "placeholder-credential",
                ["X-Goog-Date"] = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"),
                ["X-Goog-Expires"] = (expirationMinutes * 60).ToString(),
                ["X-Goog-SignedHeaders"] = "host",
                ["X-Goog-Signature"] = "placeholder-signature"
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Validates if a URL is a Google Cloud Storage signed URL
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if it's a GCS signed URL</returns>
        public static bool IsGoogleCloudStorageSignedUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                var queryParams = ParseQueryString(uri.Query);
                
                // Check if it contains GCS signed URL parameters
                return queryParams.Keys.Any(key => 
                    key.StartsWith("X-Goog-", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts signature parameters from a signed URL
        /// </summary>
        /// <param name="signedUrl">The signed URL</param>
        /// <returns>Dictionary of signature parameters</returns>
        public static Dictionary<string, string> ExtractSignatureParameters(string signedUrl)
        {
            var parameters = new Dictionary<string, string>();
            
            try
            {
                var uri = new Uri(signedUrl);
                var queryParams = ParseQueryString(uri.Query);
                
                foreach (var kvp in queryParams)
                {
                    if (kvp.Key.StartsWith("X-Goog-", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid signed URL: {ex.Message}", nameof(signedUrl));
            }
            
            return parameters;
        }

        /// <summary>
        /// Parses a query string into a dictionary
        /// </summary>
        /// <param name="queryString">The query string to parse</param>
        /// <returns>Dictionary of key-value pairs</returns>
        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var parameters = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(queryString))
                return parameters;

            // Remove the leading '?' if present
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var pairs = queryString.Split('&');
            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair))
                    continue;

                var keyValue = pair.Split(new[] { '=' }, 2);
                if (keyValue.Length == 2)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    var value = Uri.UnescapeDataString(keyValue[1]);
                    parameters[key] = value;
                }
            }

            return parameters;
        }
    }
} 