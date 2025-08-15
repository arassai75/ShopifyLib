using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Input model for staged upload creation via GraphQL
    /// Based on Shopify's stagedUploadsCreate mutation schema
    /// </summary>
    public class StagedUploadInput
    {
        /// <summary>
        /// The filename to be uploaded
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; } = "";

        /// <summary>
        /// The MIME type of the file
        /// </summary>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; } = "";

        /// <summary>
        /// The resource type (e.g., "FILE", "IMAGE", "VIDEO")
        /// </summary>
        [JsonProperty("resource")]
        public string Resource { get; set; } = "FILE";

        /// <summary>
        /// The file size in bytes (serialized as string for GraphQL compatibility)
        /// </summary>
        [JsonProperty("fileSize")]
        public string FileSize { get; set; } = "0";
    }

    /// <summary>
    /// Response model for staged upload creation
    /// </summary>
    public class StagedUploadResponse
    {
        /// <summary>
        /// The staged target URL where the file should be uploaded
        /// </summary>
        [JsonProperty("stagedTarget")]
        public StagedUploadTarget? StagedTarget { get; set; }

        /// <summary>
        /// Any user errors that occurred
        /// </summary>
        [JsonProperty("userErrors")]
        public List<UserError> UserErrors { get; set; } = new List<UserError>();
    }

    /// <summary>
    /// The staged upload target information
    /// </summary>
    public class StagedUploadTarget
    {
        /// <summary>
        /// The URL where the file should be uploaded
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; } = "";

        /// <summary>
        /// The resource URL that will be used in fileCreate mutation
        /// </summary>
        [JsonProperty("resourceUrl")]
        public string ResourceUrl { get; set; } = "";

        /// <summary>
        /// Additional parameters for the upload
        /// </summary>
        [JsonProperty("parameters")]
        public List<StagedUploadParameter> Parameters { get; set; } = new List<StagedUploadParameter>();
    }

    /// <summary>
    /// Parameter for staged upload
    /// </summary>
    public class StagedUploadParameter
    {
        /// <summary>
        /// The parameter name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// The parameter value
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; } = "";
    }
} 