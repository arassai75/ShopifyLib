using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Represents a metafield definition in Shopify GraphQL API.
    /// </summary>
    public class MetafieldDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("namespace")]
        public string Namespace { get; set; } = "";

        [JsonProperty("key")]
        public string Key { get; set; } = "";

        [JsonProperty("type")]
        public string Type { get; set; } = "";
    }
} 