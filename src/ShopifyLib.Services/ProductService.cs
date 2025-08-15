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
    /// Implementation of product-related operations
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ShopifyConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the ProductService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <param name="config">The Shopify configuration.</param>
        public ProductService(HttpClient httpClient, ShopifyConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicyHelper.SnakeCaseLower
            };
        }

        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="productId">The ID of the product to retrieve.</param>
        /// <returns>The product.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the product is not found.</exception>
        public async Task<Product> GetAsync(long productId)
        {
            var response = await _httpClient.GetAsync(string.Format("products/{0}.json", productId));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductResponse>(content, _jsonOptions);
            return result != null ? result.Product : throw new InvalidOperationException("Product not found");
        }

        /// <summary>
        /// Gets all products with optional filtering.
        /// </summary>
        /// <param name="limit">Maximum number of products to return.</param>
        /// <param name="sinceId">Return products after the specified ID.</param>
        /// <param name="title">Filter by product title.</param>
        /// <param name="vendor">Filter by product vendor.</param>
        /// <param name="handle">Filter by product handle.</param>
        /// <param name="productType">Filter by product type.</param>
        /// <param name="collectionId">Filter by collection ID.</param>
        /// <param name="createdAtMin">Filter by minimum creation date.</param>
        /// <param name="createdAtMax">Filter by maximum creation date.</param>
        /// <param name="updatedAtMin">Filter by minimum update date.</param>
        /// <param name="updatedAtMax">Filter by maximum update date.</param>
        /// <param name="publishedAtMin">Filter by minimum publish date.</param>
        /// <param name="publishedAtMax">Filter by maximum publish date.</param>
        /// <param name="publishedStatus">Filter by published status.</param>
        /// <returns>A list of products matching the criteria.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<List<Product>> GetAllAsync(
            int? limit = null,
            long? sinceId = null,
            string title = null,
            string vendor = null,
            string handle = null,
            string productType = null,
            long? collectionId = null,
            DateTime? createdAtMin = null,
            DateTime? createdAtMax = null,
            DateTime? updatedAtMin = null,
            DateTime? updatedAtMax = null,
            DateTime? publishedAtMin = null,
            DateTime? publishedAtMax = null,
            string publishedStatus = null)
        {
            var queryParams = new List<string>();

            if (limit.HasValue) queryParams.Add(string.Format("limit={0}", limit));
            if (sinceId.HasValue) queryParams.Add(string.Format("since_id={0}", sinceId));
            if (!string.IsNullOrEmpty(title)) queryParams.Add(string.Format("title={0}", Uri.EscapeDataString(title)));
            if (!string.IsNullOrEmpty(vendor)) queryParams.Add(string.Format("vendor={0}", Uri.EscapeDataString(vendor)));
            if (!string.IsNullOrEmpty(handle)) queryParams.Add(string.Format("handle={0}", Uri.EscapeDataString(handle)));
            if (!string.IsNullOrEmpty(productType)) queryParams.Add(string.Format("product_type={0}", Uri.EscapeDataString(productType)));
            if (collectionId.HasValue) queryParams.Add(string.Format("collection_id={0}", collectionId));
            if (createdAtMin.HasValue) queryParams.Add(string.Format("created_at_min={0:yyyy-MM-ddTHH:mm:ssZ}", createdAtMin.Value));
            if (createdAtMax.HasValue) queryParams.Add(string.Format("created_at_max={0:yyyy-MM-ddTHH:mm:ssZ}", createdAtMax.Value));
            if (updatedAtMin.HasValue) queryParams.Add(string.Format("updated_at_min={0:yyyy-MM-ddTHH:mm:ssZ}", updatedAtMin.Value));
            if (updatedAtMax.HasValue) queryParams.Add(string.Format("updated_at_max={0:yyyy-MM-ddTHH:mm:ssZ}", updatedAtMax.Value));
            if (publishedAtMin.HasValue) queryParams.Add(string.Format("published_at_min={0:yyyy-MM-ddTHH:mm:ssZ}", publishedAtMin.Value));
            if (publishedAtMax.HasValue) queryParams.Add(string.Format("published_at_max={0:yyyy-MM-ddTHH:mm:ssZ}", publishedAtMax.Value));
            if (!string.IsNullOrEmpty(publishedStatus)) queryParams.Add(string.Format("published_status={0}", publishedStatus));

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync("products.json" + queryString);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductsResponse>(content, _jsonOptions);
            return result != null ? result.Products : new List<Product>();
        }

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="product">The product to create.</param>
        /// <returns>The created product.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the creation fails.</exception>
        public async Task<Product> CreateAsync(Product product)
        {
            var request = new ProductRequest { Product = product };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("products.json", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Product creation failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductResponse>(responseContent, _jsonOptions);
            return result != null ? result.Product : throw new InvalidOperationException("Failed to create product");
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="productId">The ID of the product to update.</param>
        /// <param name="product">The updated product data.</param>
        /// <returns>The updated product.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the update fails.</exception>
        public async Task<Product> UpdateAsync(long productId, Product product)
        {
            var request = new ProductRequest { Product = product };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(string.Format("products/{0}.json", productId), content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProductResponse>(responseContent, _jsonOptions);
            return result != null ? result.Product : throw new InvalidOperationException("Failed to update product");
        }

        /// <summary>
        /// Deletes a product by its ID.
        /// </summary>
        /// <param name="productId">The ID of the product to delete.</param>
        /// <returns>True if the product was deleted, false otherwise.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<bool> DeleteAsync(long productId)
        {
            var response = await _httpClient.DeleteAsync(string.Format("products/{0}.json", productId));
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the total count of products.
        /// </summary>
        /// <returns>The total count of products.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<int> GetCountAsync()
        {
            var response = await _httpClient.GetAsync("products/count.json");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CountResponse>(content, _jsonOptions);
            return result != null ? result.Count : 0;
        }

        // Helper classes for JSON serialization
        private class ProductRequest
        {
            public Product Product { get; set; } = new Product();
        }

        private class ProductResponse
        {
            public Product Product { get; set; } = new Product();
        }

        private class ProductsResponse
        {
            public List<Product> Products { get; set; } = new List<Product>();
        }

        private class CountResponse
        {
            public int Count { get; set; }
        }
    }
} 