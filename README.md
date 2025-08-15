# Shopify Bulk Image Migration Library

A .NET library for bulk uploading images to Shopify from CSV files with product ID and UPC tracking using metafields.

## Quick Start

### 1. Install and Configure

```bash
git clone https://github.com/your-username/Shopify-Lib.git
cd Shopify-Lib
dotnet build
```

### 2. Create Configuration

```json
{
  "Shopify": {
    "ShopDomain": "your-shop.myshopify.com",
    "AccessToken": "your-access-token",
    "ApiVersion": "2025-04",
    "RequestsPerSecond": 2
  }
}
```

### 3. Run Bulk Migration

```csharp
// Initialize services
var client = new ShopifyClient(shopifyConfig);
var enhancedFileService = new EnhancedFileServiceWithMetadata(
    client.Files, 
    new FileMetafieldService(client.GraphQL),
    new ImageDownloadService()
);

// Process CSV in batches
var csvRows = await ReadCsvFile("products.csv");
var batches = csvRows.Chunk(50).ToList(); // 50 images per batch

for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
{
    var batch = batches[batchIndex];
    var batchId = $"{DateTime.UtcNow:yyyyMMdd}{(batchIndex + 1):D3}";
    
    // Upload batch with product ID and UPC tracking
    var response = await enhancedFileService.UploadImagesWithMetadataAsync(
        batch.Select(row => (row.ImageUrl, FileContentType.Image, row.ProductId, row.Upc, batchId, "")).ToList()
    );
    
    // Update CSV with Shopify GUIDs
    foreach (var file in response.Files)
    {
        var originalRow = batch.FirstOrDefault(r => r.ProductId == file.ProductId);
        if (originalRow != null)
        {
            originalRow.ShopifyGuid = file.Id;
            originalRow.Status = file.MetadataStored ? "Success" : "Metadata_Failed";
        }
    }
}
```

## CSV Format

### Input CSV
```csv
ProductID,UPC,ImageURL,ShopifyGUID,Status,ErrorMessage
1001,123456789012,https://example.com/image1.jpg,,,
1002,123456789013,https://example.com/image2.jpg,,,
```

### Output CSV
```csv
ProductID,UPC,ImageURL,ShopifyGUID,Status,ErrorMessage
1001,123456789012,https://example.com/image1.jpg,gid://shopify/MediaImage/123456,Success,
1002,123456789013,https://example.com/image2.jpg,gid://shopify/MediaImage/123457,Success,
```

## Key Features

- **Bulk Upload**: Upload 50-500 images per batch
- **Product ID & UPC Tracking**: Store in Shopify metafields (namespace: `migration`)
- **Batch Management**: Automatic batch ID generation
- **Error Handling**: Comprehensive error tracking and retry logic
- **Rate Limiting**: Built-in Shopify API rate limit handling
- **Content Type Support**: Image, Video, and File types

## Recommended Batch Sizes

| Batch Size | Use Case |
|------------|----------|
| **10-50** | Testing and small migrations |
| **50-200** | Production environments |
| **200-500** | Large migrations |
| **500+** | Not recommended |

## API Methods

### UploadImagesWithMetadataAsync
Upload multiple images with metadata:

```csharp
var imageData = new List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)>
{
    ("https://example.com/image1.jpg", FileContentType.Image, 1001, "123456789012", "batch_001", "Product 1001"),
    ("https://example.com/image2.jpg", FileContentType.Image, 1002, "123456789013", "batch_001", "Product 1002")
};

var response = await enhancedFileService.UploadImagesWithMetadataAsync(imageData);
```

### UploadImageWithMetadataAsync
Upload a single image:

```csharp
var response = await enhancedFileService.UploadImageWithMetadataAsync(
    "https://example.com/image.jpg",
    FileContentType.Image,
    1001, 
    "123456789012", 
    "batch_001", 
    "Product 1001"
);
```

## Response Structure

```csharp
public class FileCreateResponseWithMetadata
{
    public List<FileWithMetadata> Files { get; set; }
    public List<UserError> UserErrors { get; set; }
    public UploadSummary Summary { get; set; }
}

public class FileWithMetadata
{
    public string Id { get; set; }
    public long ProductId { get; set; }
    public string Upc { get; set; }
    public string BatchId { get; set; }
    public bool MetadataStored { get; set; }
    public string? MetadataError { get; set; }
    // ... other properties
}
```

## Metafield Storage

Product IDs and UPCs are stored in Shopify metafields:
- **Namespace**: `migration`
- **Keys**: `product_id`, `upc`, `batch_id`

**Note**: Shopify's API does not support retrieving metafields from files. Data is stored for integrity and future reference.

## Testing

```bash
dotnet test --filter "BulkBatchUpload_ShouldUploadImagesWithMetadata"
```

## Additional Features

- **User-Agent Workaround**: Handles problematic URLs (e.g., Some host requires User-agent in the header)
- **Staged Upload**: Custom headers for CDN issues
- **GraphQL Support**: Full GraphQL query and mutation support
- **Error Recovery**: Automatic retry logic with detailed reporting

