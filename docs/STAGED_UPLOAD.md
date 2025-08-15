# Staged Upload Functionality

## Overview

The staged upload functionality provides an alternative approach to uploading files to Shopify that can bypass URL download issues. This is particularly useful for URLs that require custom headers (like User-Agent), have access restrictions, or are slow to respond.

## How Staged Upload Works

The staged upload process consists of three main steps:

1. **Create Staged Upload**: Call `stagedUploadsCreate` GraphQL mutation to get a staged URL
2. **Upload File**: Upload the file directly to the staged URL using multipart form data
3. **Create File**: Use the staged resource URL in the `fileCreate` mutation

### Step-by-Step Process

```csharp
// Step 1: Create staged upload
var stagedInput = new StagedUploadInput
{
    Filename = "image.jpg",
    MimeType = "image/jpeg",
    Resource = "IMAGE",
    FileSize = imageBytes.Length
};

var stagedResponse = await graphQLService.CreateStagedUploadAsync(stagedInput);

// Step 2: Upload file to staged URL
await UploadToStagedUrlAsync(stagedResponse.StagedTarget, imageBytes, "image.jpg", "image/jpeg");

// Step 3: Create file using staged resource URL
var fileInput = new FileCreateInput
{
    OriginalSource = stagedResponse.StagedTarget.ResourceUrl,
    ContentType = FileContentType.Image,
    Alt = "Uploaded image"
};

var fileResponse = await graphQLService.CreateFilesAsync(new List<FileCreateInput> { fileInput });
```

## When to Use Staged Upload

### ✅ Use Staged Upload When:

- **URLs require custom headers** (e.g., User-Agent for Indigo images)
- **URLs have access restrictions** or require authentication
- **URLs are slow to respond** and Shopify's CDN times out
- **URLs have rate limiting** that affects Shopify's servers
- **You want more control** over the download process
- **You're uploading generated content** (not from URLs)
- **You need to modify files** before uploading

### ❌ Use Direct Upload When:

- **URLs are reliable and fast** (e.g., CDN URLs)
- **URLs don't require special headers**
- **You want simpler code** (less complexity)
- **URLs are publicly accessible** without restrictions

## Examples

### 1. Upload Indigo Image with User-Agent

```csharp
var stagedUploadService = new StagedUploadService(client.GraphQL, client.HttpClient);

var response = await stagedUploadService.UploadFileFromUrlAsync(
    url: "https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg",
    fileName: "indigo-gift-image.jpg",
    contentType: "image/jpeg",
    altText: "Indigo Gift Image",
    userAgent: "Mozilla/5.0 (compatible; Shopify-Image-Uploader/1.0)"
);
```

### 2. Upload File from Bytes

```csharp
var fileBytes = System.Text.Encoding.UTF8.GetBytes("File content here");
var response = await stagedUploadService.UploadFileAsync(
    fileBytes: fileBytes,
    fileName: "document.txt",
    contentType: "text/plain",
    altText: "Sample document"
);
```

### 3. Upload Multiple Files

```csharp
var files = new List<(byte[] Bytes, string FileName, string ContentType, string? AltText)>
{
    (bytes1, "file1.jpg", "image/jpeg", "Image 1"),
    (bytes2, "file2.pdf", "application/pdf", "Document 2")
};

var response = await stagedUploadService.UploadFilesAsync(files);
```

## API Reference

### StagedUploadService

#### Constructor
```csharp
public StagedUploadService(IGraphQLService graphQLService, HttpClient httpClient)
```

#### Methods

##### UploadFileAsync
```csharp
public async Task<FileCreateResponse> UploadFileAsync(
    byte[] fileBytes, 
    string fileName, 
    string contentType, 
    string? altText = null
)
```

##### UploadFileAsync (Stream)
```csharp
public async Task<FileCreateResponse> UploadFileAsync(
    Stream stream, 
    string fileName, 
    string contentType, 
    string? altText = null
)
```

##### UploadFileFromUrlAsync
```csharp
public async Task<FileCreateResponse> UploadFileFromUrlAsync(
    string url, 
    string fileName, 
    string contentType, 
    string? altText = null, 
    string? userAgent = null
)
```

##### UploadFilesAsync
```csharp
public async Task<FileCreateResponse> UploadFilesAsync(
    List<(byte[] Bytes, string FileName, string ContentType, string? AltText)> files
)
```

### Models

#### StagedUploadInput
```csharp
public class StagedUploadInput
{
    public string Filename { get; set; } = "";
    public string MimeType { get; set; } = "";
    public string Resource { get; set; } = "FILE";
    public long FileSize { get; set; }
}
```

#### StagedUploadResponse
```csharp
public class StagedUploadResponse
{
    public StagedUploadTarget? StagedTarget { get; set; }
    public List<UserError> UserErrors { get; set; } = new List<UserError>();
}
```

#### StagedUploadTarget
```csharp
public class StagedUploadTarget
{
    public string Url { get; set; } = "";
    public string ResourceUrl { get; set; } = "";
    public List<StagedUploadParameter> Parameters { get; set; } = new List<StagedUploadParameter>();
}
```

## Comparison with Direct Upload

| Aspect | Direct Upload | Staged Upload |
|--------|---------------|---------------|
| **Complexity** | Simple | More complex |
| **Control** | Limited | Full control |
| **URL Restrictions** | May fail | Can handle |
| **Custom Headers** | Not supported | Supported |
| **Error Handling** | Basic | Detailed |
| **Performance** | Fast for good URLs | Consistent |
| **Use Cases** | Reliable URLs | Problematic URLs |

## Troubleshooting

### Common Issues

#### 1. Staged Upload Creation Fails
**Problem**: `stagedUploadsCreate` mutation fails
**Solution**: Check file size limits and supported file types

#### 2. Upload to Staged URL Fails
**Problem**: HTTP error when uploading to staged URL
**Solution**: Verify multipart form data format and parameters

#### 3. File Creation Fails
**Problem**: `fileCreate` mutation fails with staged resource URL
**Solution**: Ensure staged upload completed successfully

### Error Handling

```csharp
try
{
    var response = await stagedUploadService.UploadFileFromUrlAsync(url, fileName, contentType, altText);
    // Handle success
}
catch (GraphQLException ex)
{
    // Handle GraphQL errors
    Console.WriteLine($"GraphQL error: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Handle HTTP errors (download or upload)
    Console.WriteLine($"HTTP error: {ex.Message}");
}
catch (Exception ex)
{
    // Handle other errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Best Practices

### 1. Use Appropriate User-Agent
```csharp
// For Indigo images
var userAgent = "Mozilla/5.0 (compatible; Shopify-Image-Uploader/1.0)";

// For other sites
var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
```

### 2. Handle Large Files
```csharp
// For large files, consider streaming
using var stream = File.OpenRead("large-file.jpg");
var response = await stagedUploadService.UploadFileAsync(stream, "large-file.jpg", "image/jpeg");
```

### 3. Batch Uploads
```csharp
// Use batch upload for multiple files
var files = new List<(byte[] Bytes, string FileName, string ContentType, string? AltText)>();
// Add files to list
var response = await stagedUploadService.UploadFilesAsync(files);
```

### 4. Error Recovery
```csharp
// Implement retry logic for failed uploads
var maxRetries = 3;
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        var response = await stagedUploadService.UploadFileFromUrlAsync(url, fileName, contentType, altText);
        break; // Success
    }
    catch (HttpRequestException ex) when (i < maxRetries - 1)
    {
        await Task.Delay(1000 * (i + 1)); // Exponential backoff
    }
}
```

## Testing

### Run Staged Upload Tests

```bash
cd tests/ShopifyLib.Tests
dotnet test --filter "StagedUploadTest"
```

### Test Specific Scenarios

```bash
# Test Indigo image upload
dotnet test --filter "UploadIndigoImage_WithStagedUpload_SuccessfullyBypassesDownloadIssues"

# Test URL upload with User-Agent
dotnet test --filter "UploadIndigoImage_FromUrl_WithStagedUpload_SuccessfullyBypassesDownloadIssues"

# Test comparison with direct upload
dotnet test --filter "CompareApproaches_StagedUploadVsDirect_ShowsAdvantages"

# Test multiple files upload
dotnet test --filter "UploadMultipleFiles_WithStagedUpload_SuccessfullyHandlesBatch"
```

## Console Example

Run the staged upload example in the console app:

```bash
cd samples/ConsoleApp
dotnet run
```

This will demonstrate:
- Uploading Indigo images with User-Agent
- Uploading multiple files
- Comparing staged vs direct upload
- Uploading files from bytes

## Conclusion

Staged upload provides a powerful alternative to direct URL uploads, especially for problematic URLs like Indigo images that require custom headers. While it adds complexity, it offers much more control and reliability for challenging upload scenarios.

Use staged upload when you need to handle URLs with restrictions, require custom headers, or want more control over the upload process. For simple, reliable URLs, direct upload remains the simpler option. 