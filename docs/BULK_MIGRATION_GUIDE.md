# Bulk Image Migration Guide

## Overview
Upload multiple images to Shopify from URLs in batches, with product ID tracking via Shopify's native metafield system.

## Recommended Batch Sizes
- **10-50**: Recommended for most use cases (fast, reliable)
- **50-200**: Production environments (good balance)
- **200-500**: Large migrations (monitor rate limits)
- **500+**: Not recommended (high failure risk)

## Rate Limits
- Default: 2 requests per second (configurable)
- Timeout: 30 seconds per request (configurable)
- Monitor API response times and adjust batch sizes accordingly

## Prerequisites
- CSV file with columns: `ProductID`, `ImageURL`, `ShopifyGUID` (empty), `Status`, `ErrorMessage`
- Shopify API credentials configured

## Process Flow

### 1. Initialize Services
```csharp
var client = new ShopifyClient(shopifyConfig);
var enhancedFileService = new EnhancedFileServiceWithMetadata(client.Files, new FileMetafieldService(client.GraphQL));
```

### 2. Read CSV Data
```csharp
var csvRows = await ReadCsvFile(csvFilePath);
```

### 3. Process in Batches
```csharp
var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
var batches = csvRows.Chunk(batchSize).ToList();

for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
{
    var batch = batches[batchIndex];
    var batchId = $"{datePrefix}{(batchIndex + 1):D3}";
    
    // Prepare image data with product IDs and batch IDs
    var imageData = batch.Select(row => (row.ImageUrl, row.ProductId, batchId)).ToList();

    // Upload batch with metafield storage
    var response = await enhancedFileService.UploadImagesWithMetadataAsync(imageData);

    // Update CSV with results
    foreach (var file in response.Files)
    {
        var originalRow = batch.FirstOrDefault(r => r.ProductId == file.ProductId);
        if (originalRow != null)
        {
            originalRow.ShopifyGuid = file.Id;
            originalRow.Status = "Success";
        }
    }
}
```

### 4. Update CSV
```csharp
await UpdateCsvFile(csvRows);
```

## Key Components

### Metafield Structure
Each uploaded image gets metafields with the following structure:
- **Namespace**: `migration`
- **Key**: `product_id` (type: number_integer)
- **Value**: The product ID from the CSV
- **Key**: `batch_id` (type: single_line_text_field)
- **Value**: The batch identifier (YYYYMMDD + 3-digit number)

### Retrieve Product ID from File
```csharp
var fileMetafieldService = new FileMetafieldService(client.GraphQL);

// Get product ID for a specific file
var productId = await fileMetafieldService.GetProductIdFromFileAsync(fileGid);

// Find all files for a specific product ID
var fileGids = await fileMetafieldService.FindFilesByProductIdAsync(productId);

// Get all metafields for a file
var metafields = await fileMetafieldService.GetFileMetafieldsAsync(fileGid);
```

### Store Product ID Metadata
```csharp
// Store product ID and batch ID metadata
await fileMetafieldService.SetProductIdMetadataAsync(fileGid, productId, batchId);

// Create custom metafield
var metafieldInput = new MetafieldInput
{
    Namespace = "migration",
    Key = "product_id",
    Value = productId.ToString(),
    Type = "number_integer"
};
await fileMetafieldService.CreateOrUpdateFileMetafieldAsync(fileGid, metafieldInput);
```

## Expected Output
- Images uploaded to Shopify dashboard
- Metafields contain product ID and batch ID for tracking
- CSV updated with Shopify GUIDs in `ShopifyGUID` column
- Status column shows "Success" or "Failed"
- Clean alt text available for accessibility purposes

## Services Used
- **EnhancedFileServiceWithMetadata** - Handles URL uploads with metafield storage
- **FileMetafieldService** - Manages metafield operations on Shopify files
- **ImageDownloadService** - Downloads images with custom headers for problematic URLs
- **GraphQLService** - Shopify GraphQL API communication

## Benefits of Metafield Approach

- **Native Shopify Feature**: Uses Shopify's built-in metafield system
- **Searchable**: Can query files by product ID using GraphQL
- **Extensible**: Easy to add additional metadata fields
- **Reliable**: No dependency on alt text or other workarounds
- **Clean Alt Text**: Alt text remains available for accessibility purposes
- **Type Safety**: Metafields have proper data types (number_integer, single_line_text_field)
- **Performance**: Efficient GraphQL queries for metadata retrieval

## Error Handling

The system includes comprehensive error handling:
- Failed uploads are tracked in the CSV
- Retry logic for transient failures
- Detailed error messages for debugging
- Batch-level error reporting
- Graceful handling of problematic image URLs

## Monitoring and Debugging

- Check metafield storage using GraphQL queries
- Verify product ID associations
- Monitor upload success rates
- Track batch processing progress
- Review error logs for failed uploads
