using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Input model for file creation via GraphQL
    /// Based on Shopify's fileCreate mutation schema
    /// </summary>
    public class FileCreateInput
    {
        /// <summary>
        /// The file content as base64 encoded string
        /// </summary>
        [JsonProperty("originalSource")]
        public string OriginalSource { get; set; } = "";

        /// <summary>
        /// The content type as an enum (IMAGE, FILE, VIDEO)
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; } = "FILE";

        /// <summary>
        /// Optional alt text for the file
        /// </summary>
        [JsonProperty("alt")]
        public string? Alt { get; set; }
    }

    /// <summary>
    /// Content type enum for file uploads
    /// </summary>
    public static class FileContentType
    {
        public const string Image = "IMAGE";
        public const string File = "FILE";
        public const string Video = "VIDEO";
    }
} 