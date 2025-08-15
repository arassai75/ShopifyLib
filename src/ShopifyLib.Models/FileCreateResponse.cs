using System.Collections.Generic;
using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Response model for file creation via GraphQL
    /// </summary>
    public class FileCreateResponse
    {
        /// <summary>
        /// The created files
        /// </summary>
        [JsonProperty("files")]
        public List<File> Files { get; set; } = new();

        /// <summary>
        /// Any user errors that occurred
        /// </summary>
        [JsonProperty("userErrors")]
        public List<UserError> UserErrors { get; set; } = new();
    }

    /// <summary>
    /// Enhanced response model for file creation with metadata tracking
    /// </summary>
    public class FileCreateResponseWithMetadata
    {
        /// <summary>
        /// The created files with their associated metadata
        /// </summary>
        public List<FileWithMetadata> Files { get; set; } = new();

        /// <summary>
        /// Any user errors that occurred
        /// </summary>
        public List<UserError> UserErrors { get; set; } = new();

        /// <summary>
        /// Summary of the upload operation
        /// </summary>
        public UploadSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// File with associated metadata for tracking
    /// </summary>
    public class FileWithMetadata : File
    {
        /// <summary>
        /// The product ID associated with this file
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// The UPC (Universal Product Code) associated with this file
        /// </summary>
        public string Upc { get; set; } = "";

        /// <summary>
        /// The batch ID associated with this file
        /// </summary>
        public string BatchId { get; set; } = "";

        /// <summary>
        /// The original image URL
        /// </summary>
        public string OriginalImageUrl { get; set; } = "";

        /// <summary>
        /// Whether metadata was successfully stored
        /// </summary>
        public bool MetadataStored { get; set; }

        /// <summary>
        /// Any error that occurred during metadata storage
        /// </summary>
        public string? MetadataError { get; set; }
    }

    /// <summary>
    /// Summary of the upload operation
    /// </summary>
    public class UploadSummary
    {
        /// <summary>
        /// Total number of files processed
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Number of files successfully uploaded
        /// </summary>
        public int SuccessfullyUploaded { get; set; }

        /// <summary>
        /// Number of files with metadata successfully stored
        /// </summary>
        public int MetadataStored { get; set; }

        /// <summary>
        /// Number of files that failed to upload
        /// </summary>
        public int FailedUploads { get; set; }

        /// <summary>
        /// Number of files that uploaded but failed metadata storage
        /// </summary>
        public int MetadataStorageFailed { get; set; }

        /// <summary>
        /// The batch ID for this upload operation
        /// </summary>
        public string BatchId { get; set; } = "";
    }

    /// <summary>
    /// Represents a file in the GraphQL response
    /// </summary>
    public class File
    {
        /// <summary>
        /// The file ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// The file status
        /// </summary>
        [JsonProperty("fileStatus")]
        public string FileStatus { get; set; } = "";

        /// <summary>
        /// The alt text for the file
        /// </summary>
        [JsonProperty("alt")]
        public string? Alt { get; set; }

        /// <summary>
        /// When the file was created
        /// </summary>
        [JsonProperty("createdAt")]
        public string CreatedAt { get; set; } = "";

        /// <summary>
        /// Image-specific properties (for MediaImage type)
        /// </summary>
        [JsonProperty("image")]
        public ImageInfo? Image { get; set; }
    }

    /// <summary>
    /// Image information for media files
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// The image width
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary>
        /// The image height
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>
        /// The Shopify CDN URL for the image
        /// </summary>
        [JsonProperty("url")]
        public string? Url { get; set; }

        /// <summary>
        /// The original source URL of the image
        /// </summary>
        [JsonProperty("originalSrc")]
        public string? OriginalSrc { get; set; }

        /// <summary>
        /// The transformed source URL (if any transformations were applied)
        /// </summary>
        [JsonProperty("transformedSrc")]
        public string? TransformedSrc { get; set; }

        /// <summary>
        /// The primary source URL (usually the same as url)
        /// </summary>
        [JsonProperty("src")]
        public string? Src { get; set; }
    }

    /// <summary>
    /// User error information
    /// </summary>
    public class UserError
    {
        /// <summary>
        /// The field that caused the error
        /// </summary>
        [JsonProperty("field")]
        public List<string> Field { get; set; } = new();

        /// <summary>
        /// The error message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = "";
    }
} 