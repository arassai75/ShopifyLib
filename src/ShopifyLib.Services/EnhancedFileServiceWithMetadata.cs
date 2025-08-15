using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Enhanced file service that stores product IDs in metafields instead of alt text
    /// </summary>
    public class EnhancedFileServiceWithMetadata
    {
        private readonly IFileService _fileService;
        private readonly IFileMetafieldService _fileMetafieldService;
        private readonly ImageDownloadService _imageDownloadService;

        /// <summary>
        /// Initializes a new instance of the EnhancedFileServiceWithMetadata class.
        /// </summary>
        /// <param name="fileService">The file service for uploading files.</param>
        /// <param name="fileMetafieldService">The file metafield service for storing metadata.</param>
        /// <param name="imageDownloadService">The image download service for handling problematic URLs.</param>
        public EnhancedFileServiceWithMetadata(
            IFileService fileService, 
            IFileMetafieldService fileMetafieldService,
            ImageDownloadService imageDownloadService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _fileMetafieldService = fileMetafieldService ?? throw new ArgumentNullException(nameof(fileMetafieldService));
            _imageDownloadService = imageDownloadService ?? throw new ArgumentNullException(nameof(imageDownloadService));
        }

        /// <summary>
        /// Uploads images from URLs with product ID and UPC metadata storage
        /// </summary>
        /// <param name="imageData">List of tuples containing (imageUrl, contentType, productId, upc, batchId, altText)</param>
        /// <returns>Upload response with metadata attached including product IDs and UPCs</returns>
        public async Task<FileCreateResponseWithMetadata> UploadImagesWithMetadataAsync(
            List<(string ImageUrl, string ContentType, long ProductId, string Upc, string BatchId, string AltText)> imageData)
        {
            if (imageData == null || imageData.Count == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

            var processedInputs = new List<FileCreateInput>();
            var metadataList = new List<(long ProductId, string Upc, string BatchId)>();

            foreach (var (imageUrl, contentType, productId, upc, batchId, altText) in imageData)
            {
                if (UserAgentWorkaround(imageUrl))
                {
                    // Download with User-Agent and convert to base64
                    var base64DataUrl = await _imageDownloadService.DownloadImageAsBase64Async(imageUrl);
                    
                    var processedInput = new FileCreateInput
                    {
                        OriginalSource = base64DataUrl,
                        ContentType = contentType,
                        Alt = altText // Use clean alt text for accessibility
                    };
                    
                    processedInputs.Add(processedInput);
                    metadataList.Add((productId, upc, batchId));
                }
                else
                {
                    // Use original input
                    var fileInput = new FileCreateInput
                    {
                        OriginalSource = imageUrl,
                        ContentType = contentType,
                        Alt = altText // Use clean alt text for accessibility
                    };
                    
                    processedInputs.Add(fileInput);
                    metadataList.Add((productId, upc, batchId));
                }
            }

            // Upload files
            var response = await _fileService.UploadFilesAsync(processedInputs);

            // Create enhanced response with metadata
            var enhancedResponse = new FileCreateResponseWithMetadata
            {
                UserErrors = response?.UserErrors ?? new List<UserError>(),
                Summary = new UploadSummary
                {
                    TotalFiles = imageData.Count,
                    BatchId = imageData.FirstOrDefault().BatchId ?? ""
                }
            };

            // Store metadata for each uploaded file and build enhanced response
            if (response?.Files != null)
            {
                for (int i = 0; i < response.Files.Count && i < metadataList.Count; i++)
                {
                    var file = response.Files[i];
                    var (productId, upc, batchId) = metadataList[i];
                    var originalImageUrl = imageData[i].ImageUrl;

                    var fileWithMetadata = new FileWithMetadata
                    {
                        Id = file.Id,
                        FileStatus = file.FileStatus,
                        Alt = file.Alt,
                        CreatedAt = file.CreatedAt,
                        Image = file.Image,
                        ProductId = productId,
                        Upc = upc,
                        BatchId = batchId,
                        OriginalImageUrl = originalImageUrl,
                        MetadataStored = false,
                        MetadataError = null
                    };

                    try
                    {
                        // Store product ID, UPC, and batch ID in metafields in a single API call
                        await _fileMetafieldService.SetProductIdAndUpcMetadataAsync(file.Id, productId, upc, batchId);
                        fileWithMetadata.MetadataStored = true;
                        enhancedResponse.Summary.MetadataStored++;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't fail the entire upload
                        Console.WriteLine($"Warning: Failed to store metadata for file {file.Id}: {ex.Message}");
                        fileWithMetadata.MetadataError = ex.Message;
                        enhancedResponse.Summary.MetadataStorageFailed++;
                    }

                    enhancedResponse.Files.Add(fileWithMetadata);
                }

                enhancedResponse.Summary.SuccessfullyUploaded = response.Files.Count;
                enhancedResponse.Summary.FailedUploads = imageData.Count - response.Files.Count;
            }

            return enhancedResponse;
        }

        /// <summary>
        /// Uploads a single image with product ID and UPC metadata
        /// </summary>
        /// <param name="imageUrl">The image URL</param>
        /// <param name="contentType">The content type of the file</param>
        /// <param name="productId">The product ID</param>
        /// <param name="upc">The UPC</param>
        /// <param name="batchId">Optional batch ID</param>
        /// <param name="altText">Optional alt text for accessibility</param>
        /// <returns>Upload response with metadata attached including product ID and UPC</returns>
        public async Task<FileCreateResponseWithMetadata> UploadImageWithMetadataAsync(
            string imageUrl,
            string contentType,
            long productId,
            string upc,
            string batchId = "", 
            string altText = "")
        {
            var imageData = new List<(string, string, long, string, string, string)>
            {
                (imageUrl, contentType, productId, upc, batchId, altText)
            };

            return await UploadImagesWithMetadataAsync(imageData);
        }

        /// <summary>
        /// Retrieves product ID from a file using metafields
        /// </summary>
        /// <param name="fileGid">The file GraphQL ID</param>
        /// <returns>The product ID if found, 0 otherwise</returns>
        public async Task<long> GetProductIdFromFileAsync(string fileGid)
        {
            return await _fileMetafieldService.GetProductIdFromFileAsync(fileGid);
        }



        /// <summary>
        /// Gets all metadata for a file
        /// </summary>
        /// <param name="fileGid">The file GraphQL ID</param>
        /// <returns>List of metafields for the file</returns>
        public async Task<List<Metafield>> GetFileMetadataAsync(string fileGid)
        {
            return await _fileMetafieldService.GetFileMetafieldsAsync(fileGid);
        }

        /// <summary>
        /// Retrieves UPC from a file using metafields
        /// </summary>
        /// <param name="fileGid">The file GraphQL ID</param>
        /// <returns>The UPC if found, empty string otherwise</returns>
        public async Task<string> GetUpcFromFileAsync(string fileGid)
        {
            return await _fileMetafieldService.GetUpcFromFileAsync(fileGid);
        }



        /// <summary>
        /// Sets product ID, UPC, and batch ID metadata on a file in a single optimized API call.
        /// This is more efficient than setting them individually.
        /// </summary>
        /// <param name="fileGid">The file GraphQL ID</param>
        /// <param name="productId">The product ID to store</param>
        /// <param name="upc">The UPC to store</param>
        /// <param name="batchId">The batch ID to store</param>
        /// <returns>List of created metafields</returns>
        public async Task<List<Metafield>> SetProductIdAndUpcMetadataAsync(string fileGid, long productId, string upc, string batchId)
        {
            return await _fileMetafieldService.SetProductIdAndUpcMetadataAsync(fileGid, productId, upc, batchId);
        }

        /// <summary>
        /// Determines if a URL should use the User-Agent workaround
        /// </summary>
        /// <param name="imageUrl">The image URL to check</param>
        /// <returns>True if the workaround should be used</returns>
        private bool UserAgentWorkaround(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            var lowerUrl = imageUrl.ToLowerInvariant();
            
            // Known domains that require User-Agent
            var problematicDomains = new[]
            {
                "indigoimages.ca",
                "indigo.ca",
                "dynamic.indigoimages.ca",
                "images.indigo.ca"
            };

            return problematicDomains.Any(domain => lowerUrl.Contains(domain));
        }
    }


}
