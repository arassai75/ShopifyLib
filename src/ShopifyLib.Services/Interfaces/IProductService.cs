using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for product-related operations
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Gets a product by ID
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <returns>The product</returns>
        Task<Product> GetAsync(long productId);

        /// <summary>
        /// Gets all products with optional filtering
        /// </summary>
        /// <param name="limit">Maximum number of products to return</param>
        /// <param name="sinceId">Return products after the specified ID</param>
        /// <param name="title">Filter by product title</param>
        /// <param name="vendor">Filter by vendor</param>
        /// <param name="handle">Filter by handle</param>
        /// <param name="productType">Filter by product type</param>
        /// <param name="collectionId">Filter by collection ID</param>
        /// <param name="createdAtMin">Filter by minimum creation date</param>
        /// <param name="createdAtMax">Filter by maximum creation date</param>
        /// <param name="updatedAtMin">Filter by minimum update date</param>
        /// <param name="updatedAtMax">Filter by maximum update date</param>
        /// <param name="publishedAtMin">Filter by minimum publish date</param>
        /// <param name="publishedAtMax">Filter by maximum publish date</param>
        /// <param name="publishedStatus">Filter by published status</param>
        /// <returns>List of products</returns>
        Task<List<Product>> GetAllAsync(
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
            string publishedStatus = null);

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <param name="product">The product to create</param>
        /// <returns>The created product</returns>
        Task<Product> CreateAsync(Product product);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="product">The updated product data</param>
        /// <returns>The updated product</returns>
        Task<Product> UpdateAsync(long productId, Product product);

        /// <summary>
        /// Deletes a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAsync(long productId);

        /// <summary>
        /// Gets the count of products
        /// </summary>
        /// <returns>The product count</returns>
        Task<int> GetCountAsync();
    }
} 