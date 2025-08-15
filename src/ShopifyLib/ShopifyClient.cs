using System;
using System.Net.Http;
using ShopifyLib.Models;
using ShopifyLib.Services;

namespace ShopifyLib
{
    /// <summary>
    /// Main client for interacting with the Shopify API
    /// </summary>
    public class ShopifyClient : IDisposable
    {
        private readonly ShopifyConfig _config;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Product-related operations
        /// </summary>
        public IProductService Products { get; }

        /// <summary>
        /// Metafield-related operations
        /// </summary>
        public IMetafieldService Metafields { get; }

        /// <summary>
        /// Image-related operations
        /// </summary>
        public IImageService Images { get; }

        /// <summary>
        /// GraphQL operations
        /// </summary>
        public IGraphQLService GraphQL { get; }

        /// <summary>
        /// File operations using GraphQL
        /// </summary>
        public IFileService Files { get; }

        /// <summary>
        /// GraphQL-based metafield operations
        /// </summary>
        public IGraphQLMetafieldService GraphQLMetafields { get; }

        /// <summary>
        /// Gets the image transformation service.
        /// </summary>
        public IImageTransformationService ImageTransformations { get; }

        /// <summary>
        /// Gets the underlying HttpClient instance
        /// </summary>
        public HttpClient HttpClient => _httpClient;

        /// <summary>
        /// Initializes a new instance of the ShopifyClient
        /// </summary>
        /// <param name="config">The Shopify configuration</param>
        public ShopifyClient(ShopifyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            if (!_config.IsValid())
            {
                throw new ArgumentException("Invalid Shopify configuration. ShopDomain and AccessToken are required.");
            }

            _httpClient = CreateHttpClient();
            
            // Initialize services
            Products = new ProductService(_httpClient, _config);
            Metafields = new MetafieldService(_httpClient, _config);
            Images = new ImageService(_httpClient, _config);
            GraphQL = new GraphQLService(_httpClient, _config);
            Files = new FileService(GraphQL, _httpClient);
            GraphQLMetafields = new GraphQLMetafieldService(GraphQL);
            ImageTransformations = new ImageTransformationService();
        }

        /// <summary>
        /// Initializes a new instance of the ShopifyClient with shop domain and access token
        /// </summary>
        /// <param name="shopDomain">The Shopify shop domain</param>
        /// <param name="accessToken">The access token</param>
        public ShopifyClient(string shopDomain, string accessToken)
            : this(new ShopifyConfig
            {
                ShopDomain = shopDomain,
                AccessToken = accessToken
            })
        {
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(string.Format("https://{0}/admin/api/{1}/", _config.ShopDomain, _config.ApiVersion)),
                Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
            };

            // Add authentication header
            client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", _config.AccessToken);

            return client;
        }

        /// <summary>
        /// Disposes the client and underlying resources
        /// </summary>
        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
} 