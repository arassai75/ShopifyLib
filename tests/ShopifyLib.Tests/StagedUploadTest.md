# StagedUploadTest Documentation

## Overview

The `StagedUploadTest` class provides comprehensive testing for Shopify's staged upload functionality, which is designed to bypass URL download issues that commonly occur when uploading images from external CDNs like Indigo Images.

## Purpose

This test suite addresses the following key challenges:
- **CDN Download Limitations**: Many external CDNs (like Indigo Images) have restrictions that prevent Shopify's standard image upload from working
- **User-Agent Requirements**: Some CDNs require specific User-Agent headers for successful downloads
- **Timeout Issues**: Large images or slow CDNs can cause upload timeouts
- **Batch Upload Support**: Testing multiple file uploads in a single operation

## Test Structure

### Class Setup
```csharp
[IntegrationTest]
public class StagedUploadTest : IDisposable
{
    private readonly ShopifyClient _client;
    private readonly StagedUploadService _stagedUploadService;
    private string? _uploadedFileId;
}
```

### Configuration
The test uses a hybrid configuration approach:
- Loads from `appsettings.json` and `appsettings.Development.json`
- Falls back to environment variables
- Validates configuration before proceeding

## Test Methods

### 1. `UploadIndigoImage_WithStagedUpload_SuccessfullyBypassesDownloadIssues()`

**Purpose**: Demonstrates how staged upload can bypass URL download issues with Indigo Images CDN.

**Key Features**:
- Downloads image with custom User-Agent header
- Implements fallback to Cloudinary demo image if Indigo fails
- Uses staged upload approach for direct file upload
- Handles both download and upload timeouts

**Test Flow**:
1. **Download Phase**: Downloads image with User-Agent header
2. **Fallback Logic**: Switches to Cloudinary if Indigo fails
3. **Upload Phase**: Uses staged upload to bypass Shopify's CDN limitations
4. **Validation**: Verifies file ID, status, and image properties

### 2. `UploadIndigoImage_FromUrl_WithStagedUpload_SuccessfullyBypassesDownloadIssues()`

**Purpose**: Tests the convenience method that handles both download and upload in one operation.

**Key Features**:
- Single method call for complete upload process
- Automatic User-Agent application during download
- Streamlined error handling

### 3. `CompareApproaches_StagedUploadVsDirect_ShowsAdvantages()`

**Purpose**: Compares staged upload vs. direct upload approaches.

**Comparison Points**:
- **Direct Upload**: Uses Shopify's standard file creation API
- **Staged Upload**: Downloads file first, then uploads to Shopify
- **Success Rates**: Shows when each approach works best
- **Error Handling**: Demonstrates different failure scenarios

### 4. `UploadMultipleFiles_WithStagedUpload_SuccessfullyHandlesBatch()`

**Purpose**: Tests batch upload functionality for multiple files.

**Features**:
- Uploads multiple images in a single operation
- Handles different file types and sizes
- Validates all files are processed correctly
- Demonstrates batch error handling

### 5. `CreateStagedUpload_ShowsParameters_ForDebugging()`

**Purpose**: Debugging test that shows staged upload parameters.

**Output**:
- Staged upload URL generation
- Required parameters for upload
- File size and content type validation
- Parameter structure for manual testing

### 6. `UploadSmallTestImage_WithStagedUpload_ShowsMultipartForm()`

**Purpose**: Demonstrates multipart form data construction for staged uploads.

**Features**:
- Shows exact multipart form structure
- Validates boundary generation
- Demonstrates file and metadata encoding
- Provides debugging information

### 7. `MinimalMultipartUpload_WorksWithShopifyStagedUrl()`

**Purpose**: Tests minimal multipart upload implementation.

**Key Points**:
- Simplified multipart form construction
- Direct HTTP upload to Shopify staged URL
- Minimal required fields
- Error handling and validation

### 8. `GenerateCurlCommand_ForManualTesting()`

**Purpose**: Generates curl commands for manual testing and debugging.

**Output**:
- Complete curl command with all parameters
- Properly formatted multipart data
- Headers and authentication
- Ready-to-use for external testing

### 9. `SampleImageUpload_EndToEnd_ShowsInShopifyDashboard()`

**Purpose**: End-to-end test that uploads a sample image visible in Shopify dashboard.

**Features**:
- Uses public sample image
- Validates dashboard visibility
- Tests complete upload workflow
- Verifies file accessibility

### 10. `SampleImageUpload_DirectToShopify_ShowsInDashboard()`

**Purpose**: Tests direct upload to Shopify without external CDN.

**Features**:
- Uploads directly to Shopify's servers
- Bypasses external CDN issues
- Validates file processing
- Tests dashboard integration

### 11. `PublicImageUpload_DirectToShopify_ShowsInDashboard()`

**Purpose**: Tests upload of public images directly to Shopify.

**Features**:
- Uses publicly accessible images
- Tests various image formats
- Validates processing pipeline
- Demonstrates dashboard integration

### 12. `DownloadIndigoImage_AndLogBytes()`

**Purpose**: Debugging test for Indigo image download issues.

**Features**:
- Downloads and logs image bytes
- Shows download progress
- Validates image integrity
- Provides debugging information

### 13. `DownloadIndigoImage_AndShowInBrowser()`

**Purpose**: Tests browser-compatible download of Indigo images.

**Features**:
- Simulates browser download
- Uses browser-like User-Agent
- Validates image accessibility
- Tests cross-platform compatibility

### 14. `DownloadIndigoImage_AndUploadToShopifyStaged()`

**Purpose**: Complete workflow test from download to staged upload.

**Features**:
- Downloads from Indigo with proper headers
- Uploads to Shopify using staged upload
- Validates complete workflow
- Tests error recovery

### 15. `DownloadCloudinaryImage_AndUploadToShopifyStaged()`

**Purpose**: Tests staged upload with Cloudinary images as fallback.

**Features**:
- Uses Cloudinary demo images
- Tests reliable image source
- Validates staged upload process
- Demonstrates fallback strategy

## Key Technical Features

### Staged Upload Process
1. **Create Staged Upload**: Generate upload URL and parameters
2. **Download File**: Download image from external source
3. **Prepare Multipart Data**: Construct multipart form data
4. **Upload to Staged URL**: Upload directly to Shopify's staged URL
5. **Validate Response**: Verify upload success and file properties

### Error Handling
- **Download Failures**: Automatic fallback to reliable sources
- **Timeout Handling**: Extended timeouts for slow CDNs
- **User-Agent Issues**: Custom headers for CDN compatibility
- **Upload Failures**: Detailed error reporting and recovery

### Multipart Form Construction
```csharp
private static ByteArrayContent BuildCustomMultipartContent(
    JsonElement parameters,
    byte[] fileData,
    string fileName,
    string boundary)
```

This method constructs the multipart form data required for staged uploads, including:
- File data with proper encoding
- Required parameters (key, policy, signature)
- Correct boundary formatting
- Content type headers

## Usage Examples

### Basic Staged Upload
```csharp
var uploadResponse = await _stagedUploadService.UploadFileAsync(
    imageBytes, 
    fileName, 
    contentType, 
    altText
);
```

### Staged Upload from URL
```csharp
var uploadResponse = await _stagedUploadService.UploadFileFromUrlAsync(
    imageUrl, 
    fileName, 
    contentType, 
    altText,
    userAgent
);
```

### Batch Upload
```csharp
var uploadResponses = await _stagedUploadService.UploadMultipleFilesAsync(
    fileInputs
);
```

## Configuration Requirements

### Required Settings
- `Shopify:ShopDomain`: Your Shopify store domain
- `Shopify:AccessToken`: Your Shopify access token

### Optional Settings
- `Shopify:ApiVersion`: API version (defaults to latest)
- `Shopify:Timeout`: Request timeout in seconds

## Best Practices

1. **Always Use User-Agent**: Set appropriate User-Agent for external CDNs
2. **Implement Fallbacks**: Have reliable fallback image sources
3. **Handle Timeouts**: Use extended timeouts for large files
4. **Validate Responses**: Always check upload response status
5. **Clean Up Resources**: Dispose of HTTP clients and streams properly

## Troubleshooting

### Common Issues
1. **Download Failures**: Check User-Agent and CDN restrictions
2. **Upload Timeouts**: Increase timeout values for large files
3. **Multipart Errors**: Verify boundary and content type formatting
4. **Authentication Errors**: Validate Shopify credentials

### Debugging Tips
1. Use the `GenerateCurlCommand_ForManualTesting()` test for manual verification
2. Check the `CreateStagedUpload_ShowsParameters_ForDebugging()` test for parameter validation
3. Use the download tests to isolate CDN issues
4. Monitor console output for detailed error information

## Integration with Other Tests

This test suite works alongside other image upload tests:
- `EnhancedImageUploadTest`: Tests enhanced GraphQL upload features
- `VariantImageTests`: Tests variant-specific image associations
- `ImageTransformationTests`: Tests image transformation features
- `LocalImageUploadTests`: Tests local file upload capabilities

## Future Enhancements

Potential improvements for this test suite:
1. **More CDN Sources**: Add tests for additional CDN providers
2. **Performance Metrics**: Add timing and performance measurements
3. **Retry Logic**: Implement automatic retry for failed uploads
4. **Parallel Uploads**: Test concurrent upload capabilities
5. **Large File Support**: Test with very large image files 