using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for metafield-related operations
    /// </summary>
    public interface IMetafieldService
    {
        /// <summary>
        /// Gets metafields for a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="namespace">Optional namespace filter</param>
        /// <param name="key">Optional key filter</param>
        /// <returns>List of metafields</returns>
        Task<List<Metafield>> GetProductMetafieldsAsync(long productId, string @namespace = null, string key = null);

        /// <summary>
        /// Gets a specific metafield
        /// </summary>
        /// <param name="metafieldId">The metafield ID</param>
        /// <returns>The metafield</returns>
        Task<Metafield> GetAsync(long metafieldId);

        /// <summary>
        /// Creates a new metafield for a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="metafield">The metafield to create</param>
        /// <returns>The created metafield</returns>
        Task<Metafield> CreateProductMetafieldAsync(long productId, Metafield metafield);

        /// <summary>
        /// Updates an existing metafield
        /// </summary>
        /// <param name="metafieldId">The metafield ID</param>
        /// <param name="metafield">The updated metafield data</param>
        /// <returns>The updated metafield</returns>
        Task<Metafield> UpdateAsync(long metafieldId, Metafield metafield);

        /// <summary>
        /// Deletes a metafield
        /// </summary>
        /// <param name="metafieldId">The metafield ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAsync(long metafieldId);

        /// <summary>
        /// Gets metafield definitions
        /// </summary>
        /// <param name="ownerResource">The owner resource type</param>
        /// <returns>List of metafield definitions</returns>
        Task<List<MetafieldDefinition>> GetDefinitionsAsync(string ownerResource = "product");

        /// <summary>
        /// Creates a metafield definition
        /// </summary>
        /// <param name="definition">The metafield definition to create</param>
        /// <returns>The created metafield definition</returns>
        Task<MetafieldDefinition> CreateDefinitionAsync(MetafieldDefinition definition);
    }
} 