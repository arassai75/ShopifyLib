using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Represents a Shopify product
    /// </summary>
    public class Product
    {
        /// <summary>
        /// The unique identifier for the product
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// The title of the product
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        /// <summary>
        /// The description of the product
        /// </summary>
        [JsonPropertyName("body_html")]
        public string BodyHtml { get; set; } = "";

        /// <summary>
        /// The vendor of the product
        /// </summary>
        [JsonPropertyName("vendor")]
        public string Vendor { get; set; } = "";

        /// <summary>
        /// The product type
        /// </summary>
        [JsonPropertyName("product_type")]
        public string ProductType { get; set; } = "";

        /// <summary>
        /// The handle (URL-friendly identifier)
        /// </summary>
        [JsonPropertyName("handle")]
        public string Handle { get; set; } = "";

        /// <summary>
        /// The status of the product (active, archived, draft)
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        /// <summary>
        /// The tags associated with the product
        /// </summary>
        [JsonPropertyName("tags")]
        public string Tags { get; set; } = "";

        /// <summary>
        /// The template suffix
        /// </summary>
        [JsonPropertyName("template_suffix")]
        public string TemplateSuffix { get; set; }

        /// <summary>
        /// Whether the product is published
        /// </summary>
        [JsonPropertyName("published")]
        public bool Published { get; set; }

        /// <summary>
        /// The published scope (web, global)
        /// </summary>
        [JsonPropertyName("published_scope")]
        public string PublishedScope { get; set; } = "";

        /// <summary>
        /// The images associated with the product
        /// </summary>
        [JsonPropertyName("images")]
        public List<ProductImage> Images { get; set; } = new List<ProductImage>();

        /// <summary>
        /// The variants of the product
        /// </summary>
        [JsonPropertyName("variants")]
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        /// <summary>
        /// The options for the product
        /// </summary>
        [JsonPropertyName("options")]
        public List<ProductOption> Options { get; set; } = new List<ProductOption>();

        /// <summary>
        /// The metafields associated with the product
        /// </summary>
        [JsonPropertyName("metafields")]
        public List<Metafield> Metafields { get; set; } = new List<Metafield>();

        /// <summary>
        /// The date when the product was created
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date when the product was last updated
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The date when the product was published
        /// </summary>
        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
    }



    /// <summary>
    /// Represents a product variant
    /// </summary>
    public class ProductVariant
    {
        /// <summary>
        /// The unique identifier for the variant
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// The product ID this variant belongs to
        /// </summary>
        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        /// <summary>
        /// The title of the variant
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        /// <summary>
        /// The price of the variant
        /// </summary>
        [JsonPropertyName("price")]
        public string Price { get; set; } = "";

        /// <summary>
        /// The SKU of the variant
        /// </summary>
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = "";

        /// <summary>
        /// The barcode of the variant
        /// </summary>
        [JsonPropertyName("barcode")]
        public string Barcode { get; set; }

        /// <summary>
        /// The weight of the variant
        /// </summary>
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        /// <summary>
        /// The weight unit of the variant
        /// </summary>
        [JsonPropertyName("weight_unit")]
        public string WeightUnit { get; set; } = "";

        /// <summary>
        /// The inventory quantity of the variant
        /// </summary>
        [JsonPropertyName("inventory_quantity")]
        public int InventoryQuantity { get; set; }

        /// <summary>
        /// The inventory management system
        /// </summary>
        [JsonPropertyName("inventory_management")]
        public string InventoryManagement { get; set; }

        /// <summary>
        /// The inventory policy
        /// </summary>
        [JsonPropertyName("inventory_policy")]
        public string InventoryPolicy { get; set; } = "";

        /// <summary>
        /// Whether the variant requires shipping
        /// </summary>
        [JsonPropertyName("requires_shipping")]
        public bool RequiresShipping { get; set; }

        /// <summary>
        /// Whether the variant is taxable
        /// </summary>
        [JsonPropertyName("taxable")]
        public bool Taxable { get; set; }

        /// <summary>
        /// The option values for the variant
        /// </summary>
        [JsonPropertyName("option1")]
        public string Option1 { get; set; }

        [JsonPropertyName("option2")]
        public string Option2 { get; set; }

        [JsonPropertyName("option3")]
        public string Option3 { get; set; }
    }

    /// <summary>
    /// Represents a product option
    /// </summary>
    public class ProductOption
    {
        /// <summary>
        /// The unique identifier for the option
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// The product ID this option belongs to
        /// </summary>
        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        /// <summary>
        /// The name of the option
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// The position of the option
        /// </summary>
        [JsonPropertyName("position")]
        public int Position { get; set; }

        /// <summary>
        /// The values for the option
        /// </summary>
        [JsonPropertyName("values")]
        public List<string> Values { get; set; } = new List<string>();
    }
} 