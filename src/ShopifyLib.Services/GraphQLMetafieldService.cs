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
    /// Implementation of GraphQL-based metafield operations
    /// </summary>
    public class GraphQLMetafieldService : IGraphQLMetafieldService
    {
        private readonly IGraphQLService _graphQLService;

        /// <summary>
        /// Initializes a new instance of the GraphQLMetafieldService class.
        /// </summary>
        /// <param name="graphQLService">The GraphQL service for executing queries.</param>
        /// <exception cref="ArgumentNullException">Thrown when graphQLService is null.</exception>
        public GraphQLMetafieldService(IGraphQLService graphQLService)
        {
            _graphQLService = graphQLService ?? throw new ArgumentNullException(nameof(graphQLService));
        }

        /// <summary>
        /// Gets metafields for a specific owner using GraphQL.
        /// </summary>
        /// <param name="ownerGid">The GraphQL ID of the owner (e.g., product, customer, order).</param>
        /// <param name="first">Maximum number of metafields to return (default: 50).</param>
        /// <returns>A list of metafields for the owner.</returns>
        /// <exception cref="ArgumentException">Thrown when ownerGid is null or empty.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<List<Metafield>> GetMetafieldsAsync(string ownerGid, int first = 50)
        {
            if (string.IsNullOrEmpty(ownerGid))
                throw new ArgumentException("Owner GID cannot be null or empty", nameof(ownerGid));

            var query = @"
                query GetMetafields($ownerId: ID!, $first: Int!) {
                    node(id: $ownerId) {
                        ... on Product {
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
                        ... on Customer {
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
                        ... on Order {
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

            var variables = new { ownerId = ownerGid, first };
            string response = null;
            try
            {
                response = await _graphQLService.ExecuteQueryAsync(query, variables);
                var result = JsonConvert.DeserializeObject<MetafieldsQueryResponse>(response);
                if (result?.Data?.Node?.Metafields?.Edges == null)
                    return new List<Metafield>();
                return result.Data.Node.Metafields.Edges
                    .Where(edge => edge?.Node != null)
                    .Select(edge => edge.Node)
                    .ToList();
            }
            catch (GraphQLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GraphQLException($"Failed to get metafields: {ex.Message}", null, response, ex);
            }
        }

        /// <summary>
        /// Creates or updates a metafield using GraphQL.
        /// </summary>
        /// <param name="metafieldInput">The input data for creating or updating the metafield.</param>
        /// <returns>The created or updated metafield.</returns>
        /// <exception cref="ArgumentNullException">Thrown when metafieldInput is null.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<Metafield> CreateOrUpdateMetafieldAsync(MetafieldInput metafieldInput)
        {
            if (metafieldInput == null)
                throw new ArgumentNullException(nameof(metafieldInput));

            var mutation = @"
                mutation MetafieldsSet($metafields: [MetafieldsSetInput!]!) {
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

            var input = new
            {
                metafields = new[]
                {
                    new
                    {
                        metafieldInput.OwnerId,
                        metafieldInput.Namespace,
                        metafieldInput.Key,
                        metafieldInput.Value,
                        metafieldInput.Type
                    }
                }
            };

            string response = null;
            try
            {
                response = await _graphQLService.ExecuteQueryAsync(mutation, input);
                var result = JsonConvert.DeserializeObject<MetafieldsSetResponse>(response);

                if (result?.Data?.MetafieldsSet?.UserErrors?.Any() == true)
                {
                    var errors = string.Join(", ", result.Data.MetafieldsSet.UserErrors.Select(e => e.Message));
                    throw new GraphQLUserException($"Failed to create/update metafield: {errors}", result.Data.MetafieldsSet.UserErrors, response);
                }

                var metafield = result?.Data?.MetafieldsSet?.Metafields?.FirstOrDefault();
                if (metafield == null)
                    throw new GraphQLException("No metafield returned from mutation", null, response);

                return metafield;
            }
            catch (GraphQLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GraphQLException($"Failed to create/update metafield: {ex.Message}", null, response, ex);
            }
        }

        /// <summary>
        /// Deletes a metafield using GraphQL.
        /// </summary>
        /// <param name="metafieldGid">The GraphQL ID of the metafield to delete.</param>
        /// <returns>True if the metafield was deleted, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown when metafieldGid is null or empty.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<bool> DeleteMetafieldAsync(string metafieldGid)
        {
            if (string.IsNullOrEmpty(metafieldGid))
                throw new ArgumentException("Metafield GID cannot be null or empty", nameof(metafieldGid));

            var mutation = @"
                mutation MetafieldDelete($id: ID!) {
                    metafieldDelete(id: $id) {
                        deletedId
                        userErrors {
                            field
                            message
                        }
                    }
                }";

            var variables = new { id = metafieldGid };
            string response = null;
            try
            {
                response = await _graphQLService.ExecuteQueryAsync(mutation, variables);
                var result = JsonConvert.DeserializeObject<MetafieldDeleteResponse>(response);

                if (result?.Data?.MetafieldDelete?.UserErrors?.Any() == true)
                {
                    var errors = string.Join(", ", result.Data.MetafieldDelete.UserErrors.Select(e => e.Message));
                    throw new GraphQLUserException($"Failed to delete metafield: {errors}", result.Data.MetafieldDelete.UserErrors, response);
                }

                // Check if the mutation was successful by looking for the deletedId
                var deletedId = result?.Data?.MetafieldDelete?.DeletedId;
                return !string.IsNullOrEmpty(deletedId) && deletedId == metafieldGid;
            }
            catch (GraphQLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GraphQLException($"Failed to delete metafield: {ex.Message}", null, response, ex);
            }
        }

        /// <summary>
        /// Gets metafield definitions for a specific owner type using GraphQL.
        /// </summary>
        /// <param name="ownerType">The type of owner for which to get definitions (e.g., "PRODUCT", "CUSTOMER", "ORDER").</param>
        /// <param name="first">Maximum number of metafield definitions to return (default: 50).</param>
        /// <returns>A list of metafield definitions for the owner type.</returns>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<List<MetafieldDefinition>> GetMetafieldDefinitionsAsync(string ownerType = "PRODUCT", int first = 50)
        {
            var query = @"
                query GetMetafieldDefinitions($ownerType: MetafieldOwnerType!, $first: Int!) {
                    metafieldDefinitions(first: $first, ownerType: $ownerType) {
                        edges {
                            node {
                                id
                                name
                                namespace
                                key
                                type
                            }
                        }
                    }
                }";

            var variables = new { ownerType, first };
            string response = null;
            try
            {
                response = await _graphQLService.ExecuteQueryAsync(query, variables);
                var result = JsonConvert.DeserializeObject<MetafieldDefinitionsQueryResponse>(response);

                if (result?.Data?.MetafieldDefinitions?.Edges == null)
                    return new List<MetafieldDefinition>();

                return result.Data.MetafieldDefinitions.Edges
                    .Where(edge => edge?.Node != null)
                    .Select(edge => edge.Node)
                    .ToList();
            }
            catch (GraphQLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GraphQLException($"Failed to get metafield definitions: {ex.Message}", null, response, ex);
            }
        }

        // Response models for GraphQL queries
        private class MetafieldsQueryResponse
        {
            [JsonProperty("data")]
            public MetafieldsQueryData? Data { get; set; }
        }

        private class MetafieldsQueryData
        {
            [JsonProperty("node")]
            public MetafieldsNode? Node { get; set; }
        }

        private class MetafieldsNode
        {
            [JsonProperty("metafields")]
            public MetafieldsConnection? Metafields { get; set; }
        }

        private class MetafieldsConnection
        {
            [JsonProperty("edges")]
            public List<MetafieldEdge>? Edges { get; set; }
        }

        private class MetafieldEdge
        {
            [JsonProperty("node")]
            public Metafield? Node { get; set; }
        }

        private class MetafieldsSetResponse
        {
            [JsonProperty("data")]
            public MetafieldsSetData? Data { get; set; }
        }

        private class MetafieldsSetData
        {
            [JsonProperty("metafieldsSet")]
            public MetafieldsSetResult? MetafieldsSet { get; set; }
        }

        private class MetafieldsSetResult
        {
            [Newtonsoft.Json.JsonProperty("metafields")]
            public List<Metafield>? Metafields { get; set; }

            [Newtonsoft.Json.JsonProperty("userErrors")]
            public List<ShopifyLib.Models.UserError>? UserErrors { get; set; }
        }

        private class MetafieldDeleteResponse
        {
            [JsonProperty("data")]
            public MetafieldDeleteData? Data { get; set; }
        }

        private class MetafieldDeleteData
        {
            [JsonProperty("metafieldDelete")]
            public MetafieldDeleteResult? MetafieldDelete { get; set; }
        }

        private class MetafieldDeleteResult
        {
            [Newtonsoft.Json.JsonProperty("deletedId")]
            public string? DeletedId { get; set; }

            [Newtonsoft.Json.JsonProperty("userErrors")]
            public List<ShopifyLib.Models.UserError>? UserErrors { get; set; }
        }

        private class MetafieldDefinitionsQueryResponse
        {
            [JsonProperty("data")]
            public MetafieldDefinitionsQueryData? Data { get; set; }
        }

        private class MetafieldDefinitionsQueryData
        {
            [JsonProperty("metafieldDefinitions")]
            public MetafieldDefinitionsConnection? MetafieldDefinitions { get; set; }
        }

        private class MetafieldDefinitionsConnection
        {
            [JsonProperty("edges")]
            public List<MetafieldDefinitionEdge>? Edges { get; set; }
        }

        private class MetafieldDefinitionEdge
        {
            [JsonProperty("node")]
            public MetafieldDefinition? Node { get; set; }
        }
    }
} 