using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for GraphQL-based metafield operations
    /// </summary>
    public interface IGraphQLMetafieldService
    {
        /// <summary>
        /// Gets metafields for a specific owner (product, customer, order, etc.)
        /// </summary>
        /// <param name="ownerGid">The GraphQL ID of the owner (e.g., "gid://shopify/Product/123456789")</param>
        /// <param name="first">Maximum number of metafields to return</param>
        /// <returns>List of metafields</returns>
        Task<List<Metafield>> GetMetafieldsAsync(string ownerGid, int first = 50);

        /// <summary>
        /// Creates or updates a metafield using the metafieldsSet mutation
        /// </summary>
        /// <param name="metafieldInput">The metafield input data</param>
        /// <returns>The created or updated metafield</returns>
        Task<Metafield> CreateOrUpdateMetafieldAsync(MetafieldInput metafieldInput);

        /// <summary>
        /// Deletes a metafield using the metafieldDelete mutation
        /// </summary>
        /// <param name="metafieldGid">The GraphQL ID of the metafield to delete</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteMetafieldAsync(string metafieldGid);

        /// <summary>
        /// Gets metafield definitions for a specific owner type
        /// </summary>
        /// <param name="ownerType">The owner type (e.g., "PRODUCT", "CUSTOMER", "ORDER")</param>
        /// <param name="first">Maximum number of definitions to return</param>
        /// <returns>List of metafield definitions</returns>
        Task<List<MetafieldDefinition>> GetMetafieldDefinitionsAsync(string ownerType = "PRODUCT", int first = 50);
    }
} 