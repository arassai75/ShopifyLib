using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for file metafield operations
    /// </summary>
    public interface IFileMetafieldService
    {
        /// <summary>
        /// Gets metafields for a specific file
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file (e.g., "gid://shopify/MediaImage/123456789")</param>
        /// <param name="first">Maximum number of metafields to return</param>
        /// <returns>List of metafields for the file</returns>
        Task<List<Metafield>> GetFileMetafieldsAsync(string fileGid, int first = 50);

        /// <summary>
        /// Creates or updates a metafield on a file using the metafieldsSet mutation
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file</param>
        /// <param name="metafieldInput">The metafield input data</param>
        /// <returns>The created or updated metafield</returns>
        Task<Metafield> CreateOrUpdateFileMetafieldAsync(string fileGid, MetafieldInput metafieldInput);

        /// <summary>
        /// Deletes a metafield from a file
        /// </summary>
        /// <param name="metafieldGid">The GraphQL ID of the metafield to delete</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteFileMetafieldAsync(string metafieldGid);

        /// <summary>
        /// Sets product ID metadata on a file
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file</param>
        /// <param name="productId">The product ID to store</param>
        /// <param name="batchId">Optional batch ID</param>
        /// <returns>The created metafield</returns>
        Task<Metafield> SetProductIdMetadataAsync(string fileGid, long productId, string batchId = "");

        /// <summary>
        /// Gets product ID metadata from a file
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file</param>
        /// <returns>The product ID if found, 0 otherwise</returns>
        Task<long> GetProductIdFromFileAsync(string fileGid);

        /// <summary>
        /// Sets UPC metadata on a file
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file</param>
        /// <param name="upc">The UPC to store</param>
        /// <param name="batchId">Optional batch ID</param>
        /// <returns>The created metafield</returns>
        Task<Metafield> SetUpcMetadataAsync(string fileGid, string upc, string batchId = "");

        /// <summary>
        /// Gets UPC metadata from a file
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file</param>
        /// <returns>The UPC if found, empty string otherwise</returns>
        Task<string> GetUpcFromFileAsync(string fileGid);

        /// <summary>
        /// Sets product ID, UPC, and batch ID metadata on a file in a single API call.
        /// This is more efficient than calling SetProductIdMetadataAsync and SetUpcMetadataAsync separately.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <param name="productId">The product ID to store.</param>
        /// <param name="upc">The UPC to store.</param>
        /// <param name="batchId">The batch ID to store.</param>
        /// <returns>List of created metafields.</returns>
        Task<List<Metafield>> SetProductIdAndUpcMetadataAsync(string fileGid, long productId, string upc, string batchId);
    }
}
