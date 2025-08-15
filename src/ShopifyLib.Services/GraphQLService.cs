using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShopifyLib.Models;
using ShopifyLib.Utils;
using Newtonsoft.Json;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Implementation of GraphQL operations
    /// </summary>
    public class GraphQLService : IGraphQLService
    {
        private readonly HttpClient _httpClient;
        private readonly ShopifyConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the GraphQLService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making GraphQL requests.</param>
        /// <param name="config">The Shopify configuration.</param>
        public GraphQLService(HttpClient httpClient, ShopifyConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Creates files using the Shopify GraphQL fileCreate mutation.
        /// </summary>
        /// <param name="files">List of files to create.</param>
        /// <returns>A FileCreateResponse containing the created files information.</returns>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        /// <exception cref="GraphQLUserException">Thrown when user errors occur.</exception>
        public async Task<FileCreateResponse> CreateFilesAsync(List<FileCreateInput> files)
        {
            const string mutation = @"
                mutation fileCreate($files: [FileCreateInput!]!) {
                    fileCreate(files: $files) {
                        files {
                            id
                            fileStatus
                            alt
                            createdAt
                            ... on MediaImage {
                                image {
                                    width
                                    height
                                    url
                                    originalSrc
                                    transformedSrc
                                    src
                                }
                                preview {
                                    image {
                                        url
                                        originalSrc
                                        transformedSrc
                                        src
                                    }
                                }
                            }
                        }
                        userErrors {
                            field
                            message
                        }
                    }
                }";

            var variables = new { files };
            var response = await ExecuteQueryAsync(mutation, variables);

            var graphQLResponse = System.Text.Json.JsonSerializer.Deserialize<GraphQLResponse<FileCreateData>>(response, _jsonOptions);

            // Check for GraphQL errors
            if (graphQLResponse?.Errors != null && graphQLResponse.Errors.Count > 0)
            {
                var errorMessages = string.Join("; ", graphQLResponse.Errors.ConvertAll(e => e.Message));
                throw new GraphQLException($"GraphQL errors: {errorMessages}", graphQLResponse.Errors, response);
            }

            // Check for user errors
            if (graphQLResponse?.Data?.FileCreate?.UserErrors != null && graphQLResponse.Data.FileCreate.UserErrors.Count > 0)
            {
                var userErrors = graphQLResponse.Data.FileCreate.UserErrors;
                var errorMessages = string.Join("; ", userErrors.ConvertAll(e => $"{string.Join(",", e.Field)}: {e.Message}"));
                throw new GraphQLUserException($"User errors: {errorMessages}", userErrors, response);
            }

            if (graphQLResponse?.Data?.FileCreate != null)
            {
                return graphQLResponse.Data.FileCreate;
            }

            throw new GraphQLException("Failed to create files via GraphQL. No data returned.", null, response);
        }

        /// <summary>
        /// Creates a staged upload using the Shopify GraphQL stagedUploadsCreate mutation.
        /// </summary>
        /// <param name="input">The staged upload input parameters.</param>
        /// <returns>A StagedUploadResponse containing the staged upload information.</returns>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        /// <exception cref="GraphQLUserException">Thrown when user errors occur.</exception>
        public async Task<StagedUploadResponse> CreateStagedUploadAsync(StagedUploadInput input)
        {
            const string mutation = @"
                mutation stagedUploadsCreate($input: [StagedUploadInput!]!) {
                    stagedUploadsCreate(input: $input) {
                        stagedTargets {
                            url
                            resourceUrl
                            parameters {
                                name
                                value
                            }
                        }
                        userErrors {
                            field
                            message
                        }
                    }
                }";

            var variables = new { input = new List<StagedUploadInput> { input } };
            var response = await ExecuteQueryAsync(mutation, variables);

            var graphQLResponse = System.Text.Json.JsonSerializer.Deserialize<GraphQLResponse<StagedUploadData>>(response, _jsonOptions);

            // Check for GraphQL errors
            if (graphQLResponse?.Errors != null && graphQLResponse.Errors.Count > 0)
            {
                var errorMessages = string.Join("; ", graphQLResponse.Errors.ConvertAll(e => e.Message));
                throw new GraphQLException($"GraphQL errors: {errorMessages}", graphQLResponse.Errors, response);
            }

            // Check for user errors
            if (graphQLResponse?.Data?.StagedUploadsCreate?.UserErrors != null && graphQLResponse.Data.StagedUploadsCreate.UserErrors.Count > 0)
            {
                var userErrors = graphQLResponse.Data.StagedUploadsCreate.UserErrors;
                var errorMessages = string.Join("; ", userErrors.ConvertAll(e => $"{string.Join(",", e.Field)}: {e.Message}"));
                throw new GraphQLUserException($"User errors: {errorMessages}", userErrors, response);
            }

            if (graphQLResponse?.Data?.StagedUploadsCreate?.StagedTargets != null && graphQLResponse.Data.StagedUploadsCreate.StagedTargets.Count > 0)
            {
                return new StagedUploadResponse
                {
                    StagedTarget = graphQLResponse.Data.StagedUploadsCreate.StagedTargets[0],
                    UserErrors = graphQLResponse.Data.StagedUploadsCreate.UserErrors ?? new List<UserError>()
                };
            }

            throw new GraphQLException("Failed to create staged upload via GraphQL. No data returned.", null, response);
        }

        /// <summary>
        /// Executes a GraphQL query or mutation.
        /// </summary>
        /// <param name="query">The GraphQL query or mutation string.</param>
        /// <param name="variables">Optional variables for the query.</param>
        /// <returns>The JSON response as a string.</returns>
        /// <exception cref="GraphQLHttpException">Thrown when HTTP errors occur.</exception>
        /// <exception cref="GraphQLException">Thrown when GraphQL errors occur.</exception>
        public async Task<string> ExecuteQueryAsync(string query, object? variables = null)
        {
            var request = new GraphQLRequest
            {
                Query = query,
                Variables = variables
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("graphql.json", content);
            }
            catch (HttpRequestException ex)
            {
                throw new GraphQLHttpException($"HTTP request failed: {ex.Message}", 0, null, ex);
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new GraphQLHttpException($"GraphQL HTTP error: {response.StatusCode} - {response.ReasonPhrase}", response.StatusCode, responseContent);
            }

            // Check for GraphQL errors in the response
            try
            {
                var errorCheck = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLResponse<object>>(responseContent);
                if (errorCheck?.Errors != null && errorCheck.Errors.Count > 0)
                {
                    var errorMessages = string.Join("; ", errorCheck.Errors.ConvertAll(e => e.Message));
                    throw new GraphQLException($"GraphQL errors: {errorMessages}", errorCheck.Errors, responseContent);
                }
            }
            catch (Exception)
            {
                // Ignore parse errors here, just return the content
            }

            return responseContent;
        }
    }

    /// <summary>
    /// GraphQL request model
    /// </summary>
    internal class GraphQLRequest
    {
        [JsonProperty("query")]
        public string Query { get; set; } = "";

        [JsonProperty("variables")]
        public object? Variables { get; set; }
    }

    /// <summary>
    /// GraphQL response wrapper
    /// </summary>
    internal class GraphQLResponse<T>
    {
        [Newtonsoft.Json.JsonProperty("data")]
        public T? Data { get; set; }

        [Newtonsoft.Json.JsonProperty("errors")]
        public List<ShopifyLib.Models.GraphQLError>? Errors { get; set; }
    }

    /// <summary>
    /// File create data wrapper
    /// </summary>
    internal class FileCreateData
    {
        [JsonProperty("fileCreate")]
        public FileCreateResponse? FileCreate { get; set; }
    }

    /// <summary>
    /// Staged upload data wrapper
    /// </summary>
    internal class StagedUploadData
    {
        [JsonProperty("stagedUploadsCreate")]
        public StagedUploadsCreateResponse? StagedUploadsCreate { get; set; }
    }

    /// <summary>
    /// Staged uploads create response
    /// </summary>
    internal class StagedUploadsCreateResponse
    {
        [JsonProperty("stagedTargets")]
        public List<StagedUploadTarget> StagedTargets { get; set; } = new List<StagedUploadTarget>();

        [JsonProperty("userErrors")]
        public List<UserError> UserErrors { get; set; } = new List<UserError>();
    }
} 