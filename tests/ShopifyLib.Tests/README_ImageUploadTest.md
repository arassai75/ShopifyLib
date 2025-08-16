# Image Upload GraphQL Test

This test demonstrates uploading an image to Shopify using the GraphQL `fileCreate` mutation and displays all response details including image dimensions and metadata.

## Test Specification

The test fulfills the following requirements:

1. ✅ **Uses the specified image URL**: `https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85`
2. ✅ **Uploads to Shopify without attaching to any product or variant**: Uses GraphQL `fileCreate` mutation
3. ✅ **Displays all Shopify response details**: Shows complete JSON response, file ID, status, dimensions, etc.
4. ✅ **Uses GraphQL service**: Leverages the `IGraphQLService` and `FileService` for upload

## What the Test Does

### 1. Image Upload Process
- Creates a `FileCreateInput` with the specified  image URL
- Uses `FileContentType.Image` to indicate it's an image file
- Sets alt text for accessibility
- Calls `_client.Files.UploadFilesAsync()` which uses GraphQL under the hood

### 2. Response Details Displayed
The test displays comprehensive information about the uploaded file:

#### Basic File Information
- **File ID**: The unique GraphQL ID (e.g., `gid://shopify/MediaImage/123456789`)
- **File Status**: Processing status (`READY`, `UPLOADED`, etc.)
- **Alt Text**: Accessibility description
- **Created At**: Timestamp of creation

#### Image Dimensions
- **Width**: Image width in pixels
- **Height**: Image height in pixels
- **Aspect Ratio**: Calculated width/height ratio

#### Shopify CDN URLs
- **Shopify CDN URL**: The main CDN URL for the image
- **Original Source**: The original source URL
- **Transformed Source**: URL with any transformations applied
- **Primary Source**: The primary source URL (usually same as CDN URL)

#### GraphQL ID Details
- **Full GraphQL ID**: Complete identifier
- **Resource Type**: Type of media (MediaImage)
- **Numeric ID**: Extracted numeric identifier

#### Complete Response
- **Full JSON Response**: Complete Shopify response in formatted JSON
- **Error Information**: Any user errors or validation issues
- **File Count**: Number of files in the response

## How to Run the Test

### Prerequisites
1. Ensure you have valid Shopify credentials in `appsettings.json`:
```json
{
  "Shopify": {
    "ShopDomain": "your-shop.myshopify.com",
    "AccessToken": "your-access-token",
    "ApiVersion": "2024-01"
  }
}
```

### Running the Test

#### Option 1: Run the Unit Test
```bash
# From the tests/ShopifyLib.Tests directory
dotnet test --filter "UploadImageToShopify_DisplayAllResponseDetails"
```

#### Option 2: Run the Console Application
```bash
# From the samples/ConsoleApp directory
dotnet run
```

#### Option 3: Run All Integration Tests
```bash
# From the tests/ShopifyLib.Tests directory
dotnet test --filter "Category=Integration"
```

## Expected Output

The test will output detailed information including:

```
=== Starting Image Upload Test ===
Image URL: https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg?width=810&maxHeight=810&quality=85
Alt Text:  Gift Image - Test Upload

🔄 Uploading image to Shopify using GraphQL...
✅ Image upload completed successfully!

=== COMPLETE SHOPIFY RESPONSE DETAILS ===
{
  "files": [
    {
      "id": "gid://shopify/MediaImage/123456789",
      "fileStatus": "READY",
      "alt": " Gift Image - Test Upload",
      "createdAt": "2024-01-15T10:30:00Z",
      "image": {
        "width": 810,
        "height": 810
      }
    }
  ],
  "userErrors": []
}

=== UPLOADED FILE DETAILS ===
📁 File ID: gid://shopify/MediaImage/123456789
📊 File Status: READY
📝 Alt Text:  Gift Image - Test Upload
📅 Created At: 2024-01-15T10:30:00Z

=== IMAGE DIMENSIONS ===
📏 Width: 810 pixels
📐 Height: 810 pixels
📊 Aspect Ratio: 1.00

=== SHOPIFY CDN URLS ===
🌐 Shopify CDN URL: https://cdn.shopify.com/s/files/1/1234/5678/files/image.jpg
🔗 Original Source: https://dynamic.images.ca/v1/gifts/gifts/673419406239/1.jpg
🔄 Transformed Source: https://cdn.shopify.com/s/files/1/1234/5678/files/image.jpg
📷 Primary Source: https://cdn.shopify.com/s/files/1/1234/5678/files/image.jpg

=== FILE STATUS INFORMATION ===
🔄 Processing Status: READY
✅ File is ready for use!

=== GRAPHQL ID INFORMATION ===
🆔 Full GraphQL ID: gid://shopify/MediaImage/123456789
🏷️ Resource Type: MediaImage
🔢 Numeric ID: 123456789

=== ADDITIONAL METADATA ===
📋 Response contains 1 file(s)
❌ User Errors: 0

=== TEST SUMMARY ===
✅ Successfully uploaded image to Shopify
✅ File ID: gid://shopify/MediaImage/123456789
✅ Status: READY
✅ Dimensions: 810x810
✅ Shopify CDN URL: https://cdn.shopify.com/s/files/1/1234/5678/files/image.jpg
✅ Primary Source: https://cdn.shopify.com/s/files/1/1234/5678/files/image.jpg
✅ Image uploaded without attaching to any product or variant
✅ All response details displayed above
```

## Technical Details

### GraphQL Mutation Used
The test uses the Shopify GraphQL `fileCreate` mutation:
```graphql
mutation fileCreate($files: [FileCreateInput!]!) {
  fileCreate(files: $files) {
    files {
      id
      fileStatus
      alt
      createdAt
      ... on MediaImage {
        image {
          width
          height
          url
          originalSrc
          transformedSrc
          src
        }
      }
    }
    userErrors {
      field
      message
    }
  }
}
```

### Key Features
- **URL-based upload**: Uses the image URL directly without downloading
- **No product attachment**: Image is uploaded to Shopify's media library only
- **Complete metadata**: Retrieves all available file information
- **Error handling**: Displays any user errors or validation issues
- **Formatted output**: Easy-to-read console output with emojis and sections

### Files Created
1. `tests/ShopifyLib.Tests/ImageUploadGraphQLTest.cs` - Main test file
2. `samples/ConsoleApp/ImageUploadExample.cs` - Standalone example class
3. Updated `samples/ConsoleApp/Program.cs` - Added demo method
4. `tests/ShopifyLib.Tests/README_ImageUploadTest.md` - This documentation

## Troubleshooting

### Common Issues
1. **Invalid credentials**: Ensure your Shopify API credentials are correct
2. **Network issues**: The image URL must be publicly accessible
3. **API permissions**: Ensure your access token has file upload permissions
4. **Rate limiting**: Shopify may throttle requests if too many are made

### Error Messages
- `GraphQL errors`: Check your GraphQL query syntax
- `User errors`: Validation issues with the input data
- `Network errors`: Connectivity or URL accessibility issues

## Notes
- The image is uploaded to Shopify's media library but not attached to any product
- The test uses the GraphQL service as specified in the requirements
- All response details are displayed in a comprehensive, readable format
- The test includes proper error handling and validation 