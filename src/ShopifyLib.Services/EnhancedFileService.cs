using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Enhanced file service that automatically handles User-Agent issues
    /// </summary>
    public class EnhancedFileService
    {
        private readonly IFileService _fileService;
        private readonly ImageDownloadService _imageDownloadService;

        public EnhancedFileService(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _imageDownloadService = new ImageDownloadService();
        }

        /// <summary>
        /// Uploads files with automatic User-Agent handling for problematic URLs
        /// </summary>
        /// <param name="fileInputs">List of file inputs to upload</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadFilesWithUserAgentHandlingAsync(List<FileCreateInput> fileInputs)
        {
            if (fileInputs == null || fileInputs.Count == 0)
                throw new ArgumentException("File inputs cannot be null or empty", nameof(fileInputs));

            var processedInputs = new List<FileCreateInput>();

            foreach (var input in fileInputs)
            {
                if (UserAgentWorkaround(input.OriginalSource))
                {
                    // Download with User-Agent and convert to base64
                    var base64DataUrl = await _imageDownloadService.DownloadImageAsBase64Async(input.OriginalSource);
                    
                    var processedInput = new FileCreateInput
                    {
                        OriginalSource = base64DataUrl,
                        ContentType = input.ContentType,
                        Alt = input.Alt
                    };
                    
                    processedInputs.Add(processedInput);
                }
                else
                {
                    // Use original input
                    processedInputs.Add(input);
                }
            }

            return await _fileService.UploadFilesAsync(processedInputs);
        }

        /// <summary>
        /// Uploads a single file with automatic User-Agent handling
        /// </summary>
        /// <param name="fileInput">File input to upload</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadFileWithUserAgentHandlingAsync(FileCreateInput fileInput)
        {
            if (fileInput == null)
                throw new ArgumentNullException(nameof(fileInput));

            return await UploadFilesWithUserAgentHandlingAsync(new List<FileCreateInput> { fileInput });
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

            // Check if it's a URL (not base64 data)
            if (imageUrl.StartsWith("data:"))
                return false;

            // Check if it's a known problematic domain
            return ImageDownloadService.RequiresUserAgent(imageUrl);
        }

        /// <summary>
        /// Uploads an image from URL with automatic User-Agent handling
        /// </summary>
        /// <param name="imageUrl">The image URL</param>
        /// <param name="altText">Alt text for the image</param>
        /// <param name="contentType">Content type (default: Image)</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadImageFromUrlAsync(string imageUrl, string altText, string contentType = FileContentType.Image)
        {
            if (string.IsNullOrEmpty(imageUrl))
                throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));

            var fileInput = new FileCreateInput
            {
                OriginalSource = imageUrl,
                ContentType = contentType,
                Alt = altText
            };

            return await UploadFileWithUserAgentHandlingAsync(fileInput);
        }

        /// <summary>
        /// Uploads multiple images from URLs with automatic User-Agent handling
        /// </summary>
        /// <param name="imageUrls">List of image URLs and their metadata</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadImagesFromUrlsAsync(List<(string Url, string AltText)> imageUrls)
        {
            if (imageUrls == null || imageUrls.Count == 0)
                throw new ArgumentException("Image URLs cannot be null or empty", nameof(imageUrls));

            var fileInputs = new List<FileCreateInput>();

            foreach (var (url, altText) in imageUrls)
            {
                fileInputs.Add(new FileCreateInput
                {
                    OriginalSource = url,
                    ContentType = FileContentType.Image,
                    Alt = altText
                });
            }

            return await UploadFilesWithUserAgentHandlingAsync(fileInputs);
        }

        /// <summary>
        /// Disposes the underlying services
        /// </summary>
        public void Dispose()
        {
            _imageDownloadService?.Dispose();
        }
    }
} 