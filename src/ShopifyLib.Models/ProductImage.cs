using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Represents a product image in Shopify API.
    /// </summary>
    public class ProductImage
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("product_id")]
        public long ProductId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; } = "";

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; } = "";

        [JsonProperty("variant_ids")]
        public List<long> VariantIds { get; set; } = new List<long>();
    }
} 