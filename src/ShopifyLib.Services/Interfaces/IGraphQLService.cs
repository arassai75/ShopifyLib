using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for GraphQL operations
    /// </summary>
    public interface IGraphQLService
    {
        /// <summary>
        /// Creates files using the GraphQL fileCreate mutation
        /// </summary>
        /// <param name="files">The files to create</param>
        /// <returns>The file creation response</returns>
        Task<FileCreateResponse> CreateFilesAsync(List<FileCreateInput> files);

        /// <summary>
        /// Executes a custom GraphQL query or mutation
        /// </summary>
        /// <param name="query">The GraphQL query or mutation</param>
        /// <param name="variables">The variables for the query</param>
        /// <returns>The JSON response as a string</returns>
        Task<string> ExecuteQueryAsync(string query, object? variables = null);

        /// <summary>
        /// Creates a staged upload using the GraphQL stagedUploadsCreate mutation
        /// </summary>
        /// <param name="input">The staged upload input parameters</param>
        /// <returns>The staged upload response</returns>
        Task<StagedUploadResponse> CreateStagedUploadAsync(StagedUploadInput input);
    }
} 