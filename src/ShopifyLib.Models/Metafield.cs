using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Represents a metafield returned by Shopify API.
    /// Handles both REST API (numeric IDs) and GraphQL API (string GIDs).
    /// </summary>
    public class Metafield
    {
        [JsonProperty("id")]
        [System.Text.Json.Serialization.JsonConverter(typeof(MetafieldIdConverter))]
        public string Id { get; set; } = "";

        [JsonProperty("namespace")]
        public string Namespace { get; set; } = "";

        [JsonProperty("key")]
        public string Key { get; set; } = "";

        [JsonProperty("value")]
        [System.Text.Json.Serialization.JsonConverter(typeof(MetafieldValueConverter))]
        public string Value { get; set; } = "";

        [JsonProperty("type")]
        public string Type { get; set; } = "";
    }

    /// <summary>
    /// Custom JSON converter for Metafield ID that handles both numeric (REST) and string (GraphQL) IDs
    /// </summary>
    public class MetafieldIdConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // REST API returns numeric ID
                var numericId = reader.GetInt64();
                return numericId.ToString();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                // GraphQL API returns string GID
                return reader.GetString() ?? "";
            }
            
            throw new System.Text.Json.JsonException($"Unexpected token type {reader.TokenType} for Metafield ID");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    /// <summary>
    /// Custom JSON converter for Metafield Value that handles both string and numeric values
    /// </summary>
    public class MetafieldValueConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // REST API can return numeric values
                if (reader.TryGetInt64(out var intValue))
                {
                    return intValue.ToString();
                }
                else if (reader.TryGetDouble(out var doubleValue))
                {
                    return doubleValue.ToString();
                }
                else
                {
                    return reader.GetDecimal().ToString();
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                // Most common case - string value
                return reader.GetString() ?? "";
            }
            else if (reader.TokenType == JsonTokenType.True)
            {
                return "true";
            }
            else if (reader.TokenType == JsonTokenType.False)
            {
                return "false";
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return "";
            }
            
            throw new System.Text.Json.JsonException($"Unexpected token type {reader.TokenType} for Metafield Value");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
} 