using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ShopifyLib.Models;
using Newtonsoft.Json;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Implementation of file metafield operations
    /// </summary>
    public class FileMetafieldService : IFileMetafieldService
    {
        private readonly IGraphQLService _graphQLService;

        /// <summary>
        /// Initializes a new instance of the FileMetafieldService class.
        /// </summary>
        /// <param name="graphQLService">The GraphQL service for executing queries.</param>
        /// <exception cref="ArgumentNullException">Thrown when graphQLService is null.</exception>
        public FileMetafieldService(IGraphQLService graphQLService)
        {
            _graphQLService = graphQLService ?? throw new ArgumentNullException(nameof(graphQLService));
        }

        /// <summary>
        /// Gets metafields for a specific file using GraphQL.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <param name="first">Maximum number of metafields to return (default: 50).</param>
        /// <returns>A list of metafields for the file.</returns>
        /// <exception cref="ArgumentException">Thrown when fileGid is null or empty.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<List<Metafield>> GetFileMetafieldsAsync(string fileGid, int first = 50)
        {
            if (string.IsNullOrEmpty(fileGid))
                throw new ArgumentException("File GID cannot be null or empty", nameof(fileGid));

            var query = @"
                query GetFileMetafields($fileId: ID!, $first: Int!) {
                    node(id: $fileId) {
                        ... on MediaImage {
                            metafields(first: $first) {
                                edges {
                                    node {
                                        id
                                        namespace
                                        key
                                        value
                                        type
                                    }
                                }
                            }
                        }
                    }
                }";

            var variables = new { fileId = fileGid, first };
            var response = await _graphQLService.ExecuteQueryAsync(query, variables);
            
            // Use JObject for better parsing
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
            var node = jsonResponse["data"]?["node"];
            
            var metafields = new List<Metafield>();
            
            if (node?["metafields"] != null)
            {
                var metafieldsNode = node["metafields"];
                var edges = metafieldsNode["edges"];
                
                if (edges != null)
                {
                    foreach (var edge in edges)
                    {
                        var metafieldNode = edge["node"];
                        
                        if (metafieldNode != null)
                        {
                            var metafield = new Metafield
                            {
                                Id = metafieldNode["id"]?.ToString() ?? "",
                                Namespace = metafieldNode["namespace"]?.ToString() ?? "",
                                Key = metafieldNode["key"]?.ToString() ?? "",
                                Value = metafieldNode["value"]?.ToString() ?? "",
                                Type = metafieldNode["type"]?.ToString() ?? ""
                            };
                            
                            metafields.Add(metafield);
                        }
                    }
                }
            }

            return metafields;
        }

        /// <summary>
        /// Creates or updates a metafield on a file using the metafieldsSet mutation.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <param name="metafieldInput">The metafield input data.</param>
        /// <returns>The created or updated metafield.</returns>
        /// <exception cref="ArgumentException">Thrown when fileGid is null or empty.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<Metafield> CreateOrUpdateFileMetafieldAsync(string fileGid, MetafieldInput metafieldInput)
        {
            if (string.IsNullOrEmpty(fileGid))
                throw new ArgumentException("File GID cannot be null or empty", nameof(fileGid));

            var mutation = @"
                mutation metafieldsSet($metafields: [MetafieldsSetInput!]!) {
                    metafieldsSet(metafields: $metafields) {
                        metafields {
                            id
                            namespace
                            key
                            value
                            type
                        }
                        userErrors {
                            field
                            message
                        }
                    }
                }";

            var metafieldSetInput = new
            {
                ownerId = fileGid,
                @namespace = metafieldInput.Namespace,
                key = metafieldInput.Key,
                value = metafieldInput.Value,
                type = metafieldInput.Type
            };

            var variables = new { metafields = new[] { metafieldSetInput } };
            var response = await _graphQLService.ExecuteQueryAsync(mutation, variables);
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
            
            var userErrors = jsonResponse["data"]?["metafieldsSet"]?["userErrors"];
            if (userErrors != null && userErrors.Count() > 0)
            {
                var errorMessages = new List<string>();
                foreach (var error in userErrors)
                {
                    var field = error["field"]?.ToString() ?? "";
                    var message = error["message"]?.ToString() ?? "";
                    errorMessages.Add($"{field}: {message}");
                }
                throw new GraphQLException($"Metafield creation failed: {string.Join(", ", errorMessages)}");
            }

            var metafield = jsonResponse["data"]?["metafieldsSet"]?["metafields"]?[0];
            if (metafield == null)
                throw new InvalidOperationException("Failed to create metafield - no metafield returned");

            return new Metafield
            {
                Id = metafield["id"]?.ToString() ?? "",
                Namespace = metafield["namespace"]?.ToString() ?? "",
                Key = metafield["key"]?.ToString() ?? "",
                Value = metafield["value"]?.ToString() ?? "",
                Type = metafield["type"]?.ToString() ?? ""
            };
        }

        /// <summary>
        /// Deletes a metafield from a file.
        /// </summary>
        /// <param name="metafieldGid">The GraphQL ID of the metafield to delete.</param>
        /// <returns>True if the metafield was deleted successfully.</returns>
        /// <exception cref="ArgumentException">Thrown when metafieldGid is null or empty.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<bool> DeleteFileMetafieldAsync(string metafieldGid)
        {
            if (string.IsNullOrEmpty(metafieldGid))
                throw new ArgumentException("Metafield GID cannot be null or empty", nameof(metafieldGid));

            var mutation = @"
                mutation metafieldDelete($input: MetafieldDeleteInput!) {
                    metafieldDelete(input: $input) {
                        deletedMetafieldId
                        userErrors {
                            field
                            message
                        }
                    }
                }";

            var variables = new { input = new { id = metafieldGid } };
            var response = await _graphQLService.ExecuteQueryAsync(mutation, variables);
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);

            var userErrors = jsonResponse["data"]?["metafieldDelete"]?["userErrors"];
            if (userErrors != null && userErrors.Count() > 0)
            {
                var errorMessages = new List<string>();
                foreach (var error in userErrors)
                {
                    var field = error["field"]?.ToString() ?? "";
                    var message = error["message"]?.ToString() ?? "";
                    errorMessages.Add($"{field}: {message}");
                }
                throw new GraphQLException($"Metafield deletion failed: {string.Join(", ", errorMessages)}");
            }

            var deletedMetafieldId = jsonResponse["data"]?["metafieldDelete"]?["deletedMetafieldId"]?.ToString();
            return !string.IsNullOrEmpty(deletedMetafieldId);
        }

        /// <summary>
        /// Sets product ID metadata on a file.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <param name="productId">The product ID to store.</param>
        /// <param name="batchId">Optional batch ID.</param>
        /// <returns>The created metafield.</returns>
        public async Task<Metafield> SetProductIdMetadataAsync(string fileGid, long productId, string batchId = "")
        {
            var metafieldInput = new MetafieldInput
            {
                Namespace = "migration",
                Key = "product_id",
                Value = productId.ToString(),
                Type = "number_integer"
            };

            var metafield = await CreateOrUpdateFileMetafieldAsync(fileGid, metafieldInput);

            // If batch ID is provided, also store it
            if (!string.IsNullOrEmpty(batchId))
            {
                var batchMetafieldInput = new MetafieldInput
                {
                    Namespace = "migration",
                    Key = "batch_id",
                    Value = batchId,
                    Type = "single_line_text_field"
                };

                await CreateOrUpdateFileMetafieldAsync(fileGid, batchMetafieldInput);
            }

            return metafield;
        }

        /// <summary>
        /// Gets product ID metadata from a file.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <returns>The product ID if found, 0 otherwise.</returns>
        public async Task<long> GetProductIdFromFileAsync(string fileGid)
        {
            var metafields = await GetFileMetafieldsAsync(fileGid);
            var productIdMetafield = metafields.FirstOrDefault(m => 
                m.Namespace == "migration" && m.Key == "product_id");

            if (productIdMetafield != null && long.TryParse(productIdMetafield.Value, out var productId))
            {
                return productId;
            }

            return 0;
        }



        /// <summary>
        /// Sets UPC metadata on a file.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <param name="upc">The UPC to store.</param>
        /// <param name="batchId">Optional batch ID.</param>
        /// <returns>The created metafield.</returns>
        public async Task<Metafield> SetUpcMetadataAsync(string fileGid, string upc, string batchId = "")
        {
            var metafieldInput = new MetafieldInput
            {
                Namespace = "migration",
                Key = "upc",
                Value = upc,
                Type = "single_line_text_field"
            };

            var metafield = await CreateOrUpdateFileMetafieldAsync(fileGid, metafieldInput);

            // If batch ID is provided, also store it
            if (!string.IsNullOrEmpty(batchId))
            {
                var batchMetafieldInput = new MetafieldInput
                {
                    Namespace = "migration",
                    Key = "batch_id",
                    Value = batchId,
                    Type = "single_line_text_field"
                };

                await CreateOrUpdateFileMetafieldAsync(fileGid, batchMetafieldInput);
            }

            return metafield;
        }

        /// <summary>
        /// Sets product ID, UPC, and batch ID metadata on a file in a single API call.
        /// This is more efficient than calling SetProductIdMetadataAsync and SetUpcMetadataAsync separately.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <param name="productId">The product ID to store.</param>
        /// <param name="upc">The UPC to store.</param>
        /// <param name="batchId">The batch ID to store.</param>
        /// <returns>List of created metafields.</returns>
        public async Task<List<Metafield>> SetProductIdAndUpcMetadataAsync(string fileGid, long productId, string upc, string batchId)
        {
            if (string.IsNullOrEmpty(fileGid))
                throw new ArgumentException("File GID cannot be null or empty", nameof(fileGid));

            var mutation = @"
                mutation metafieldsSet($metafields: [MetafieldsSetInput!]!) {
                    metafieldsSet(metafields: $metafields) {
                        metafields {
                            id
                            namespace
                            key
                            value
                            type
                        }
                        userErrors {
                            field
                            message
                        }
                    }
                }";

            // Create all metafields in a single mutation
            var metafieldSetInputs = new[]
            {
                new
                {
                    ownerId = fileGid,
                    @namespace = "migration",
                    key = "product_id",
                    value = productId.ToString(),
                    type = "number_integer"
                },
                new
                {
                    ownerId = fileGid,
                    @namespace = "migration",
                    key = "upc",
                    value = upc,
                    type = "single_line_text_field"
                },
                new
                {
                    ownerId = fileGid,
                    @namespace = "migration",
                    key = "batch_id",
                    value = batchId,
                    type = "single_line_text_field"
                }
            };

            var variables = new { metafields = metafieldSetInputs };
            var response = await _graphQLService.ExecuteQueryAsync(mutation, variables);
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
            
            var userErrors = jsonResponse["data"]?["metafieldsSet"]?["userErrors"];
            if (userErrors != null && userErrors.Count() > 0)
            {
                var errorMessages = new List<string>();
                foreach (var error in userErrors)
                {
                    var field = error["field"]?.ToString() ?? "";
                    var message = error["message"]?.ToString() ?? "";
                    errorMessages.Add($"{field}: {message}");
                }
                throw new GraphQLException($"Metafield creation failed: {string.Join(", ", errorMessages)}");
            }

            var metafields = jsonResponse["data"]?["metafieldsSet"]?["metafields"];
            if (metafields == null)
                throw new InvalidOperationException("Failed to create metafields - no metafields returned");

            var result = new List<Metafield>();
            foreach (var metafield in metafields)
            {
                result.Add(new Metafield
                {
                    Id = metafield["id"]?.ToString() ?? "",
                    Namespace = metafield["namespace"]?.ToString() ?? "",
                    Key = metafield["key"]?.ToString() ?? "",
                    Value = metafield["value"]?.ToString() ?? "",
                    Type = metafield["type"]?.ToString() ?? ""
                });
            }

            return result;
        }

        /// <summary>
        /// Gets UPC metadata from a file.
        /// </summary>
        /// <param name="fileGid">The GraphQL ID of the file.</param>
        /// <returns>The UPC if found, empty string otherwise.</returns>
        public async Task<string> GetUpcFromFileAsync(string fileGid)
        {
            var metafields = await GetFileMetafieldsAsync(fileGid);
            var upcMetafield = metafields.FirstOrDefault(m => 
                m.Namespace == "migration" && m.Key == "upc");

            return upcMetafield?.Value ?? "";
        }


    }
}
