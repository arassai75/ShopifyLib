using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Input for creating or updating a metafield via GraphQL.
    /// </summary>
    public class MetafieldInput
    {
        [JsonProperty("namespace")]
        public string Namespace { get; set; } = "";

        [JsonProperty("key")]
        public string Key { get; set; } = "";

        [JsonProperty("value")]
        public string Value { get; set; } = "";

        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("ownerId")]
        public string OwnerId { get; set; } = ""; // GID, e.g. "gid://shopify/Product/123456789"
    }
} 