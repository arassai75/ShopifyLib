# Getting Shopify CDN URLs with the First Call

## Overview

This document explains how to obtain Shopify CDN URLs immediately with the first API call, without waiting for processing or making additional queries.

## Problem Statement

When uploading images to Shopify using GraphQL, CDN URLs are often not available in the initial response:

```json
{
  "files": [
    {
      "id": "gid://shopify/MediaImage/31257007849649",
      "fileStatus": "UPLOADED",
      "image": null  // ❌ CDN URLs not available
    }
  ]
}
```

This requires either:
- Waiting for processing to complete
- Making additional queries to get the CDN URLs
- Implementing polling mechanisms

## Solution: REST API Approach

The **REST API** provides CDN URLs immediately with the first call, making it the preferred method for applications that need immediate access to CDN URLs.

### Test Results

Our tests demonstrate the difference between GraphQL and REST API approaches:

| Method | CDN URL in First Call | Status | Example |
|--------|----------------------|---------|---------|
| **REST API** | ✅ **YES** | Immediate | `https://cdn.shopify.com/s/files/1/0655/8980/5233/files/1_78776c26-9351-43ff-8d84-5f1a284ffbcd.jpg?v=1752697208` |
| **GraphQL** | ❌ **NO** | Requires waiting/querying | `Not available` |

### Test with Indigo Dynamic URL

Using the Indigo dynamic URL: `https://dynamic.indigoimages.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85`

**REST API Results:**
```
🎉 SUCCESS: CDN URL obtained with FIRST call!
🌐 CDN URL: https://cdn.shopify.com/s/files/1/0655/8980/5233/files/1_78776c26-9351-43ff-8d84-5f1a284ffbcd.jpg?v=1752697208
📏 Dimensions: 810x810 pixels
✅ CDN URL is accessible (Status: OK)
```

## Implementation

### Method 1: REST API (Recommended for Immediate CDN URLs)

```csharp
public async Task<string> UploadImageAndGetCDNUrlAsync(string imageUrl, string altText)
{
    // Step 1: Create a temporary product
    var tempProduct = new Product
    {
        Title = $"Temp Product for CDN Test {DateTime.UtcNow:yyyyMMddHHmmss}",
        BodyHtml = "<p>Temporary product for getting CDN URL</p>",
        Vendor = "Test Vendor",
        ProductType = "Test Type",
        Status = "draft",
        Published = false
    };

    var createdProduct = await _client.Products.CreateAsync(tempProduct);

    try
    {
        // Step 2: Upload image via REST API (gets CDN URL immediately)
        var restImage = await _client.Images.UploadImageFromUrlAsync(
            createdProduct.Id,
            imageUrl,
            altText,
            1
        );

        // Step 3: Get the CDN URL from the FIRST call
        var cdnUrl = restImage.Src; // ✅ Available immediately!

        return cdnUrl;
    }
    finally
    {
        // Step 4: Clean up the temporary product
        await _client.Products.DeleteAsync(createdProduct.Id);
    }
}
```

### Method 2: GraphQL with Wait and Query

```csharp
public async Task<string> UploadImageAndGetCDNUrlAsync_GraphQL(string imageUrl, string altText)
{
    // Step 1: Upload via GraphQL
    var fileInput = new FileCreateInput
    {
        OriginalSource = imageUrl,
        ContentType = FileContentType.Image,
        Alt = altText
    };

    var response = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
    var uploadedFile = response.Files[0];

    // Step 2: Wait for processing
    await Task.Delay(2000); // Wait 2 seconds

    // Step 3: Query the file to get CDN URLs
    var fileQuery = @"
        query getFile($id: ID!) {
            node(id: $id) {
                ... on MediaImage {
                    id
                    fileStatus
                    image {
                        url
                        originalSrc
                        transformedSrc
                        src
                    }
                }
            }
        }";

    var queryVariables = new { id = uploadedFile.Id };
    var queryResponse = await _client.GraphQL.ExecuteQueryAsync(fileQuery, queryVariables);

    // Parse response to get CDN URL
    var parsedQuery = JsonConvert.DeserializeObject<dynamic>(queryResponse);
    var node = parsedQuery?.data?.node;
    
    return node?.image?.url?.ToString() ?? node?.image?.src?.ToString();
}
```

### Method 3: Hybrid Approach

```csharp
public async Task<(string GraphQLId, string CdnUrl)> UploadImageHybridAsync(string imageUrl, string altText)
{
    // Step 1: Upload via GraphQL (creates file in Shopify system)
    var fileInput = new FileCreateInput
    {
        OriginalSource = imageUrl,
        ContentType = FileContentType.Image,
        Alt = altText
    };

    var graphqlResponse = await _client.Files.UploadFilesAsync(new List<FileCreateInput> { fileInput });
    var graphqlFile = graphqlResponse.Files[0];

    // Step 2: Upload same image via REST to get immediate CDN URL
    var tempProduct = new Product
    {
        Title = $"Temp Product {DateTime.UtcNow:yyyyMMddHHmmss}",
        Status = "draft",
        Published = false
    };

    var createdProduct = await _client.Products.CreateAsync(tempProduct);

    try
    {
        var restImage = await _client.Images.UploadImageFromUrlAsync(
            createdProduct.Id,
            imageUrl,
            altText,
            1
        );

        return (graphqlFile.Id, restImage.Src);
    }
    finally
    {
        await _client.Products.DeleteAsync(createdProduct.Id);
    }
}
```

## API Comparison

### REST API (`_client.Images.UploadImageFromUrlAsync`)

**Pros:**
- ✅ CDN URL available immediately
- ✅ Works with all image URLs (including Indigo dynamic URLs)
- ✅ No waiting or additional queries needed
- ✅ CDN URL is accessible right away

**Cons:**
- ❌ Requires creating a temporary product
- ❌ Image is attached to a product (not standalone)
- ❌ Requires cleanup of temporary product

### GraphQL API (`_client.Files.UploadFilesAsync`)

**Pros:**
- ✅ Creates standalone files in Shopify's media library
- ✅ No temporary products needed
- ✅ Better for file management workflows

**Cons:**
- ❌ CDN URLs not available in first call
- ❌ Requires waiting for processing
- ❌ May need additional queries to get URLs

## Use Cases

### When to Use REST API (Immediate CDN URLs)

- **E-commerce applications** that need CDN URLs immediately
- **Content management systems** requiring instant image access
- **Real-time applications** where waiting is not acceptable
- **Image processing workflows** that depend on immediate URL access

### When to Use GraphQL (Standalone Files)

- **File management systems** that don't need immediate URLs
- **Background processing** where waiting is acceptable
- **Media library management** applications
- **Bulk upload operations** where timing is flexible

### When to Use Hybrid Approach

- **Applications requiring both** immediate CDN URLs and standalone files
- **Complex workflows** that need both product images and media library files
- **Migration scenarios** where both approaches are needed

## Best Practices

### 1. Choose the Right Method

```csharp
// For immediate CDN URL access
if (needImmediateCdnUrl)
{
    return await UploadImageAndGetCDNUrlAsync(imageUrl, altText);
}

// For standalone file management
else
{
    return await UploadImageAndGetCDNUrlAsync_GraphQL(imageUrl, altText);
}
```

### 2. Handle Errors Gracefully

```csharp
public async Task<string> UploadImageWithFallbackAsync(string imageUrl, string altText)
{
    try
    {
        // Try REST API first for immediate CDN URL
        return await UploadImageAndGetCDNUrlAsync(imageUrl, altText);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"REST API failed: {ex.Message}");
        
        // Fallback to GraphQL approach
        return await UploadImageAndGetCDNUrlAsync_GraphQL(imageUrl, altText);
    }
}
```

### 3. Implement Proper Cleanup

```csharp
public async Task<string> UploadImageWithCleanupAsync(string imageUrl, string altText)
{
    Product tempProduct = null;
    
    try
    {
        tempProduct = await CreateTemporaryProductAsync();
        var cdnUrl = await UploadImageViaRestAsync(tempProduct.Id, imageUrl, altText);
        return cdnUrl;
    }
    finally
    {
        if (tempProduct != null)
        {
            await _client.Products.DeleteAsync(tempProduct.Id);
        }
    }
}
```

## Testing

### Running the Tests

```bash
# Test REST API with immediate CDN URL
dotnet test --filter "UploadImage_GetCDNUrl_FirstCall_REST_API"

# Test GraphQL with wait and query
dotnet test --filter "UploadImage_GetShopifyCDNUrl_DisplayAllAttributes"
```

### Expected Results

**REST API Test:**
```
🎉 SUCCESS: CDN URL obtained with FIRST call!
🌐 CDN URL: https://cdn.shopify.com/s/files/1/0655/8980/5233/files/1_78776c26-9351-43ff-8d84-5f1a284ffbcd.jpg?v=1752697208
✅ CDN URL is accessible (Status: OK)
```

**GraphQL Test:**
```
⚠️  Image details not available in response
ℹ️  This is normal for newly uploaded files - Shopify needs time to process the image
```

## Troubleshooting

### Common Issues

1. **REST API fails with timeout**
   - Check if the image URL is accessible
   - Verify the URL doesn't require special headers
   - Try with a different image URL

2. **GraphQL doesn't return CDN URLs**
   - This is expected behavior
   - Use the wait and query approach
   - Consider switching to REST API

3. **CDN URL not accessible immediately**
   - This is normal for newly created URLs
   - Wait a few minutes for CDN propagation
   - Test the URL again after a delay

### Error Handling

```csharp
public async Task<string> UploadImageWithErrorHandlingAsync(string imageUrl, string altText)
{
    try
    {
        return await UploadImageAndGetCDNUrlAsync(imageUrl, altText);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Network error: {ex.Message}");
        throw;
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"Upload failed: {ex.Message}");
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
        throw;
    }
}
```

## Conclusion

For **immediate CDN URL access**, use the **REST API approach**. It provides CDN URLs with the first call and works reliably with various image sources, including Indigo dynamic URLs.

For **standalone file management**, use the **GraphQL approach** but be prepared to wait or make additional queries for CDN URLs.

The choice between methods depends on your specific requirements for timing, file management, and application architecture.

## Related Documentation

- [Image Upload Issues and Solutions](../tests/ShopifyLib.Tests/IMAGE_UPLOAD_ISSUES_AND_SOLUTIONS.md)
- [Staged Upload Documentation](./STAGED_UPLOAD.md)
- [GraphQL File Upload](../README.md#graphql-file-upload) 