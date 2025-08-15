using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Represents a metafield in Shopify GraphQL API.
    /// </summary>
    public class GraphQLMetafield
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("namespace")]
        public string Namespace { get; set; } = "";

        [JsonProperty("key")]
        public string Key { get; set; } = "";

        [JsonProperty("value")]
        public string Value { get; set; } = "";

        [JsonProperty("type")]
        public string Type { get; set; } = "";
    }
} 