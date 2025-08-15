using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using ShopifyLib.Services;
using ShopifyLib.Services.Interfaces;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Tests for Google Cloud Storage signed URL uploads
    /// </summary>
    public class GoogleCloudStorageUploadTest
    {
        private readonly IGoogleCloudStorageService _gcsService;

        public GoogleCloudStorageUploadTest()
        {
            var httpClient = new HttpClient();
            _gcsService = new GoogleCloudStorageService(httpClient);
        }

        [Fact]
        public async Task UploadToSignedUrl_WithXGoogSignature_ShouldSucceed()
        {
            // This is an example of how to use a Google Cloud Storage signed URL
            // In practice, you would get this URL from your backend or Google Cloud Console
            
            // Example signed URL (this is a placeholder - you need a real one)
            var signedUrl = "https://storage.googleapis.com/your-bucket/your-object?X-Goog-Algorithm=GOOG4-RSA-SHA256&X-Goog-Credential=your-credential&X-Goog-Date=20240101T120000Z&X-Goog-Expires=3600&X-Goog-SignedHeaders=host&X-Goog-Signature=your-signature";
            
            // Test image bytes (1x1 pixel JPEG)
            var testImageBytes = new byte[]
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48,
                0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08,
                0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
                0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20,
                0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27,
                0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
                0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01, 0xFF, 0xC4, 0x00, 0x14,
                0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x08, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x0C, 0x03, 0x01, 0x00, 0x02,
                0x11, 0x03, 0x11, 0x00, 0x3F, 0x00, 0x8A, 0x00, 0x07, 0xFF, 0xD9
            };

            var fileName = "test-image.jpg";
            var contentType = "image/jpeg";

            Console.WriteLine("=== GOOGLE CLOUD STORAGE SIGNED URL UPLOAD TEST ===");
            Console.WriteLine($"Signed URL: {signedUrl}");
            Console.WriteLine($"File Name: {fileName}");
            Console.WriteLine($"Content Type: {contentType}");
            Console.WriteLine($"File Size: {testImageBytes.Length} bytes");
            Console.WriteLine();

            // Extract and display signature parameters
            var signatureParams = GoogleCloudStorageService.ExtractSignatureParameters(signedUrl);
            Console.WriteLine("=== SIGNATURE PARAMETERS ===");
            foreach (var param in signatureParams)
            {
                Console.WriteLine($"{param.Key}: {param.Value}");
            }
            Console.WriteLine();

            // Check if it's a valid GCS signed URL
            var isValidGcsUrl = GoogleCloudStorageService.IsGoogleCloudStorageSignedUrl(signedUrl);
            Console.WriteLine($"Is Valid GCS Signed URL: {isValidGcsUrl}");
            Console.WriteLine();

            // Note: This will fail with a placeholder URL, but shows the structure
            try
            {
                var response = await _gcsService.UploadToSignedUrlAsync(signedUrl, testImageBytes, contentType, fileName);
                
                Console.WriteLine($"Upload Response Status: {response.StatusCode}");
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Upload Response: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Upload to GCS signed URL successful!");
                }
                else
                {
                    Console.WriteLine($"❌ Upload failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload failed with exception: {ex.Message}");
                Console.WriteLine("Note: This is expected with a placeholder signed URL");
            }
        }

        [Fact]
        public void ExtractSignatureParameters_FromSignedUrl_ShouldReturnParameters()
        {
            // Example signed URL with all parameters
            var signedUrl = "https://storage.googleapis.com/test-bucket/test-object?X-Goog-Algorithm=GOOG4-RSA-SHA256&X-Goog-Credential=test@test.iam.gserviceaccount.com%2F20240101%2Fauto%2Fstorage%2Fgoog4_request&X-Goog-Date=20240101T120000Z&X-Goog-Expires=3600&X-Goog-SignedHeaders=host&X-Goog-Signature=abc123def456";

            var parameters = GoogleCloudStorageService.ExtractSignatureParameters(signedUrl);

            Assert.NotNull(parameters);
            Assert.Equal(6, parameters.Count);
            Assert.True(parameters.ContainsKey("X-Goog-Algorithm"));
            Assert.True(parameters.ContainsKey("X-Goog-Credential"));
            Assert.True(parameters.ContainsKey("X-Goog-Date"));
            Assert.True(parameters.ContainsKey("X-Goog-Expires"));
            Assert.True(parameters.ContainsKey("X-Goog-SignedHeaders"));
            Assert.True(parameters.ContainsKey("X-Goog-Signature"));

            Console.WriteLine("=== EXTRACTED PARAMETERS ===");
            foreach (var param in parameters)
            {
                Console.WriteLine($"{param.Key}: {param.Value}");
            }
        }

        [Fact]
        public void IsGoogleCloudStorageSignedUrl_WithValidUrl_ShouldReturnTrue()
        {
            var validUrl = "https://storage.googleapis.com/bucket/object?X-Goog-Algorithm=GOOG4-RSA-SHA256&X-Goog-Signature=abc123";
            var invalidUrl = "https://storage.googleapis.com/bucket/object";
            var nonGcsUrl = "https://example.com/file.jpg";

            Assert.True(GoogleCloudStorageService.IsGoogleCloudStorageSignedUrl(validUrl));
            Assert.False(GoogleCloudStorageService.IsGoogleCloudStorageSignedUrl(invalidUrl));
            Assert.False(GoogleCloudStorageService.IsGoogleCloudStorageSignedUrl(nonGcsUrl));
        }

        [Fact]
        public async Task UploadFromStream_ToSignedUrl_ShouldWork()
        {
            // Create a test stream
            var testData = "Hello, Google Cloud Storage!";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testData));
            
            // Example signed URL (placeholder)
            var signedUrl = "https://storage.googleapis.com/test-bucket/test-file.txt?X-Goog-Algorithm=GOOG4-RSA-SHA256&X-Goog-Signature=abc123";
            
            var fileName = "test-file.txt";
            var contentType = "text/plain";

            Console.WriteLine("=== STREAM UPLOAD TEST ===");
            Console.WriteLine($"Signed URL: {signedUrl}");
            Console.WriteLine($"File Name: {fileName}");
            Console.WriteLine($"Content Type: {contentType}");
            Console.WriteLine($"Stream Length: {stream.Length} bytes");
            Console.WriteLine();

            try
            {
                var response = await _gcsService.UploadToSignedUrlAsync(signedUrl, stream, contentType, fileName);
                Console.WriteLine($"Upload Response Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Stream upload failed: {ex.Message}");
                Console.WriteLine("Note: This is expected with a placeholder signed URL");
            }
        }
    }
} 