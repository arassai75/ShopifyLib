using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShopifyLib.Models;
using ShopifyLib.Utils;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Implementation of metafield-related operations
    /// </summary>
    public class MetafieldService : IMetafieldService
    {
        private readonly HttpClient _httpClient;
        private readonly ShopifyConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the MetafieldService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <param name="config">The Shopify configuration.</param>
        public MetafieldService(HttpClient httpClient, ShopifyConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicyHelper.SnakeCaseLower
            };
        }

        /// <summary>
        /// Gets metafields for a product with optional filtering.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="namespace">Optional namespace filter.</param>
        /// <param name="key">Optional key filter.</param>
        /// <returns>A list of metafields matching the criteria.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<List<Metafield>> GetProductMetafieldsAsync(long productId, string @namespace = null, string key = null)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(@namespace)) queryParams.Add(string.Format("namespace={0}", Uri.EscapeDataString(@namespace)));
            if (!string.IsNullOrEmpty(key)) queryParams.Add(string.Format("key={0}", Uri.EscapeDataString(key)));

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync(string.Format("products/{0}/metafields.json{1}", productId, queryString));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MetafieldsResponse>(content, _jsonOptions);
            return result != null ? result.Metafields : new List<Metafield>();
        }

        /// <summary>
        /// Gets a metafield by its ID.
        /// </summary>
        /// <param name="metafieldId">The ID of the metafield to retrieve.</param>
        /// <returns>The metafield.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the metafield is not found.</exception>
        public async Task<Metafield> GetAsync(long metafieldId)
        {
            var response = await _httpClient.GetAsync(string.Format("metafields/{0}.json", metafieldId));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MetafieldResponse>(content, _jsonOptions);
            return result != null ? result.Metafield : throw new InvalidOperationException("Metafield not found");
        }

        /// <summary>
        /// Creates a metafield for a product.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="metafield">The metafield to create.</param>
        /// <returns>The created metafield.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the creation fails.</exception>
        public async Task<Metafield> CreateProductMetafieldAsync(long productId, Metafield metafield)
        {
            var request = new MetafieldRequest { Metafield = metafield };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Format("products/{0}/metafields.json", productId), content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MetafieldResponse>(responseContent, _jsonOptions);
            return result != null ? result.Metafield : throw new InvalidOperationException("Failed to create metafield");
        }

        /// <summary>
        /// Updates a metafield.
        /// </summary>
        /// <param name="metafieldId">The ID of the metafield to update.</param>
        /// <param name="metafield">The updated metafield data.</param>
        /// <returns>The updated metafield.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the update fails.</exception>
        public async Task<Metafield> UpdateAsync(long metafieldId, Metafield metafield)
        {
            var request = new MetafieldRequest { Metafield = metafield };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(string.Format("metafields/{0}.json", metafieldId), content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MetafieldResponse>(responseContent, _jsonOptions);
            return result != null ? result.Metafield : throw new InvalidOperationException("Failed to update metafield");
        }

        /// <summary>
        /// Deletes a metafield by its ID.
        /// </summary>
        /// <param name="metafieldId">The ID of the metafield to delete.</param>
        /// <returns>True if the metafield was deleted, false otherwise.</returns>
        public async Task<bool> DeleteAsync(long metafieldId)
        {
            var response = await _httpClient.DeleteAsync(string.Format("metafields/{0}.json", metafieldId));
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets metafield definitions for a specific owner resource.
        /// </summary>
        /// <param name="ownerResource">The owner resource type (e.g., "product", "customer", "order").</param>
        /// <returns>A list of metafield definitions.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<List<MetafieldDefinition>> GetDefinitionsAsync(string ownerResource = "product")
        {
            var response = await _httpClient.GetAsync(string.Format("metafield_definitions.json?owner_resource={0}", ownerResource));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MetafieldDefinitionsResponse>(content, _jsonOptions);
            return result != null ? result.MetafieldDefinitions : new List<MetafieldDefinition>();
        }

        /// <summary>
        /// Creates a new metafield definition.
        /// </summary>
        /// <param name="definition">The metafield definition to create.</param>
        /// <returns>The created metafield definition.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the creation fails.</exception>
        public async Task<MetafieldDefinition> CreateDefinitionAsync(MetafieldDefinition definition)
        {
            var request = new MetafieldDefinitionRequest { MetafieldDefinition = definition };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("metafield_definitions.json", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MetafieldDefinitionResponse>(responseContent, _jsonOptions);
            return result != null ? result.MetafieldDefinition : throw new InvalidOperationException("Failed to create metafield definition");
        }

        // Helper classes for JSON serialization
        private class MetafieldRequest
        {
            public Metafield Metafield { get; set; } = new Metafield();
        }

        private class MetafieldResponse
        {
            public Metafield Metafield { get; set; } = new Metafield();
        }

        private class MetafieldsResponse
        {
            public List<Metafield> Metafields { get; set; } = new List<Metafield>();
        }

        private class MetafieldDefinitionRequest
        {
            public MetafieldDefinition MetafieldDefinition { get; set; } = new MetafieldDefinition();
        }

        private class MetafieldDefinitionResponse
        {
            public MetafieldDefinition MetafieldDefinition { get; set; } = new MetafieldDefinition();
        }

        private class MetafieldDefinitionsResponse
        {
            public List<MetafieldDefinition> MetafieldDefinitions { get; set; } = new List<MetafieldDefinition>();
        }
    }
} 