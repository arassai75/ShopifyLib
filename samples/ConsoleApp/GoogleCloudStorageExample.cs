using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ShopifyLib.Services;
using ShopifyLib.Services.Interfaces;

namespace ConsoleApp
{
    /// <summary>
    /// Example demonstrating how to use Google Cloud Storage signed URLs with x-goog-signature
    /// </summary>
    public class GoogleCloudStorageExample
    {
        private readonly IGoogleCloudStorageService _gcsService;

        public GoogleCloudStorageExample()
        {
            var httpClient = new HttpClient();
            _gcsService = new GoogleCloudStorageService(httpClient);
        }

        /// <summary>
        /// Demonstrates how to upload to a Google Cloud Storage signed URL
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== GOOGLE CLOUD STORAGE SIGNED URL UPLOAD EXAMPLE ===");
            Console.WriteLine();

            // Step 1: Get a signed URL (in practice, this comes from your backend)
            var signedUrl = await GetSignedUrlFromBackendAsync();
            
            if (string.IsNullOrEmpty(signedUrl))
            {
                Console.WriteLine("❌ No signed URL available. Please configure your backend to generate signed URLs.");
                return;
            }

            Console.WriteLine("✅ Got signed URL from backend");
            Console.WriteLine($"URL: {signedUrl}");
            Console.WriteLine();

            // Step 2: Analyze the signed URL parameters
            AnalyzeSignedUrl(signedUrl);
            Console.WriteLine();

            // Step 3: Prepare file for upload
            var fileBytes = CreateTestFile();
            var fileName = "example-image.jpg";
            var contentType = "image/jpeg";

            Console.WriteLine($"📁 File Details:");
            Console.WriteLine($"   Name: {fileName}");
            Console.WriteLine($"   Type: {contentType}");
            Console.WriteLine($"   Size: {fileBytes.Length:N0} bytes");
            Console.WriteLine();

            // Step 4: Upload to GCS using the signed URL
            await UploadToGoogleCloudStorageAsync(signedUrl, fileBytes, contentType, fileName);
        }

        /// <summary>
        /// Simulates getting a signed URL from your backend
        /// In practice, this would be an API call to your server
        /// </summary>
        private async Task<string> GetSignedUrlFromBackendAsync()
        {
            Console.WriteLine("🔄 Getting signed URL from backend...");
            
            // Simulate API call delay
            await Task.Delay(100);
            
            // In practice, this would be something like:
            // var response = await httpClient.GetAsync("https://your-backend.com/api/gcs/signed-url?bucket=my-bucket&object=uploads/image.jpg");
            // return await response.Content.ReadAsStringAsync();
            
            // For demonstration, return a placeholder URL
            // Replace this with your actual signed URL
            return "https://storage.googleapis.com/your-bucket/your-object?X-Goog-Algorithm=GOOG4-RSA-SHA256&X-Goog-Credential=your-service-account%2F20240101%2Fauto%2Fstorage%2Fgoog4_request&X-Goog-Date=20240101T120000Z&X-Goog-Expires=3600&X-Goog-SignedHeaders=host&X-Goog-Signature=your-actual-signature";
        }

        /// <summary>
        /// Analyzes a signed URL and extracts its parameters
        /// </summary>
        private void AnalyzeSignedUrl(string signedUrl)
        {
            Console.WriteLine("🔍 Analyzing signed URL parameters...");
            
            try
            {
                var parameters = GoogleCloudStorageService.ExtractSignatureParameters(signedUrl);
                
                Console.WriteLine("📋 Extracted Parameters:");
                foreach (var param in parameters)
                {
                    Console.WriteLine($"   {param.Key}: {param.Value}");
                }
                
                // Validate the URL
                var isValid = GoogleCloudStorageService.IsGoogleCloudStorageSignedUrl(signedUrl);
                Console.WriteLine($"✅ Valid GCS Signed URL: {isValid}");
                
                // Check expiration
                if (parameters.TryGetValue("X-Goog-Expires", out var expiresStr) && 
                    int.TryParse(expiresStr, out var expiresSeconds))
                {
                    var expiresMinutes = expiresSeconds / 60;
                    Console.WriteLine($"⏰ URL expires in: {expiresMinutes} minutes");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing signed URL: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a test file for upload demonstration
        /// </summary>
        private byte[] CreateTestFile()
        {
            // Create a simple 1x1 pixel JPEG image
            return new byte[]
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
        }

        /// <summary>
        /// Uploads a file to Google Cloud Storage using the signed URL
        /// </summary>
        private async Task UploadToGoogleCloudStorageAsync(string signedUrl, byte[] fileBytes, string contentType, string fileName)
        {
            Console.WriteLine("🚀 Uploading to Google Cloud Storage...");
            
            try
            {
                var response = await _gcsService.UploadToSignedUrlAsync(signedUrl, fileBytes, contentType, fileName);
                
                Console.WriteLine($"📊 Upload Response:");
                Console.WriteLine($"   Status Code: {response.StatusCode}");
                Console.WriteLine($"   Is Success: {response.IsSuccessStatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Upload successful!");
                    
                    // Get the final URL (remove query parameters)
                    var uri = new Uri(signedUrl);
                    var finalUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                    Console.WriteLine($"🌐 File available at: {finalUrl}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Upload failed: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload failed with exception: {ex.Message}");
                Console.WriteLine("💡 Make sure you have a valid signed URL with proper x-goog-signature");
            }
        }

        /// <summary>
        /// Shows how to generate a signed URL on the backend (server-side)
        /// This is just for reference - you would implement this on your server
        /// </summary>
        public static void ShowBackendImplementation()
        {
            Console.WriteLine("=== BACKEND IMPLEMENTATION REFERENCE ===");
            Console.WriteLine();
            Console.WriteLine("To generate signed URLs on your backend, you need:");
            Console.WriteLine();
            Console.WriteLine("1. Google Cloud SDK or Google.Cloud.Storage.V1 NuGet package");
            Console.WriteLine("2. Service account credentials with Storage Object Admin role");
            Console.WriteLine("3. Implementation similar to this:");
            Console.WriteLine();
            Console.WriteLine(@"
// C# Backend Example (using Google Cloud SDK)
public async Task<string> GenerateSignedUrlAsync(string bucketName, string objectName)
{
    var storage = StorageClient.Create();
    var credential = GoogleCredential.FromFile(""path/to/service-account-key.json"");
    
    var urlSigner = UrlSigner.FromCredential(credential);
    var signedUrl = await urlSigner.SignAsync(
        bucketName, 
        objectName, 
        TimeSpan.FromMinutes(60), 
        HttpMethod.Put);
    
    return signedUrl;
}
");
            Console.WriteLine();
            Console.WriteLine("The signed URL will include all necessary x-goog-* parameters");
            Console.WriteLine("including the x-goog-signature that authenticates the request.");
        }
    }
} 