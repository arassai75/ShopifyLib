using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Service for uploading local images to Shopify Media space
    /// </summary>
    public class LocalImageUploadService
    {
        private readonly IFileService _fileService;

        public LocalImageUploadService(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <summary>
        /// Uploads a local image file to Shopify Media space
        /// </summary>
        /// <param name="filePath">Path to the local image file</param>
        /// <param name="altText">Alt text for the image</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadLocalImageAsync(string filePath, string altText = "")
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"Image file not found: {filePath}");

            // Validate file extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!IsValidImageExtension(extension))
                throw new ArgumentException($"Unsupported image format: {extension}. Supported formats: jpg, jpeg, png, gif, webp");

            // Read file and convert to base64
#if NETFRAMEWORK
            var imageBytes = System.IO.File.ReadAllBytes(filePath);
#else
            var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
#endif
            var base64String = Convert.ToBase64String(imageBytes);
            
            // Determine content type
            var contentType = GetContentTypeFromExtension(extension);
            var dataUrl = $"data:{contentType};base64,{base64String}";

            // Create file input
            var fileInput = new FileCreateInput
            {
                OriginalSource = dataUrl,
                ContentType = FileContentType.Image,
                Alt = altText
            };

            return await _fileService.UploadFilesAsync(new List<FileCreateInput> { fileInput });
        }

        /// <summary>
        /// Uploads multiple local image files to Shopify Media space
        /// </summary>
        /// <param name="filePaths">List of file paths and their alt text</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadLocalImagesAsync(List<(string FilePath, string AltText)> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0)
                throw new ArgumentException("File paths cannot be null or empty", nameof(filePaths));

            var fileInputs = new List<FileCreateInput>();

            foreach (var (filePath, altText) in filePaths)
            {
                if (string.IsNullOrEmpty(filePath))
                    continue;

                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File not found: {filePath}");
                    continue;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (!IsValidImageExtension(extension))
                {
                    Console.WriteLine($"Warning: Unsupported format: {filePath}");
                    continue;
                }

#if NETFRAMEWORK
                var imageBytes = System.IO.File.ReadAllBytes(filePath);
#else
                var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
#endif
                var base64String = Convert.ToBase64String(imageBytes);
                var contentType = GetContentTypeFromExtension(extension);
                var dataUrl = $"data:{contentType};base64,{base64String}";

                fileInputs.Add(new FileCreateInput
                {
                    OriginalSource = dataUrl,
                    ContentType = FileContentType.Image,
                    Alt = altText
                });
            }

            if (fileInputs.Count == 0)
                throw new InvalidOperationException("No valid image files found to upload");

            return await _fileService.UploadFilesAsync(fileInputs);
        }

        /// <summary>
        /// Uploads all images from a directory to Shopify Media space
        /// </summary>
        /// <param name="directoryPath">Path to the directory containing images</param>
        /// <param name="searchPattern">Search pattern (default: "*.jpg;*.jpeg;*.png;*.gif;*.webp")</param>
        /// <param name="altTextPrefix">Prefix for alt text (default: "Local Image")</param>
        /// <returns>Upload response</returns>
        public async Task<FileCreateResponse> UploadImagesFromDirectoryAsync(string directoryPath, string searchPattern = "*.jpg;*.jpeg;*.png;*.gif;*.webp", string altTextPrefix = "Local Image")
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            var filePaths = new List<(string FilePath, string AltText)>();
#if NETFRAMEWORK
            var patterns = searchPattern.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
#else
            var patterns = searchPattern.Split(';', StringSplitOptions.RemoveEmptyEntries);
#endif

            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var altText = string.IsNullOrEmpty(altTextPrefix) ? fileName : $"{altTextPrefix} - {fileName}";
                    filePaths.Add((file, altText));
                }
            }

            if (filePaths.Count == 0)
                throw new InvalidOperationException($"No image files found in directory: {directoryPath}");

            return await UploadLocalImagesAsync(filePaths);
        }

        /// <summary>
        /// Validates if a file extension is a supported image format
        /// </summary>
        /// <param name="extension">File extension (with or without dot)</param>
        /// <returns>True if the extension is supported</returns>
        private bool IsValidImageExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            // Remove dot if present
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            var validExtensions = new[] { "jpg", "jpeg", "png", "gif", "webp" };
            return Array.Exists(validExtensions, x => x.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the MIME content type from file extension
        /// </summary>
        /// <param name="extension">File extension (with or without dot)</param>
        /// <returns>MIME content type</returns>
        private string GetContentTypeFromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "image/jpeg";

            // Remove dot if present
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            return extension.ToLowerInvariant() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "webp" => "image/webp",
                _ => "image/jpeg" // Default fallback
            };
        }

        /// <summary>
        /// Gets file information for a local image
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        /// <returns>File information</returns>
        public async Task<LocalImageInfo> GetImageInfoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"Image file not found: {filePath}");

            var fileInfo = new System.IO.FileInfo(filePath);
#if NETFRAMEWORK
            var imageBytes = System.IO.File.ReadAllBytes(filePath);
#else
            var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
#endif

            return new LocalImageInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSize = fileInfo.Length,
                FileSizeInMB = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2),
                Extension = Path.GetExtension(filePath),
                ContentType = GetContentTypeFromExtension(Path.GetExtension(filePath)),
                IsValidImage = IsValidImageExtension(Path.GetExtension(filePath)),
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime
            };
        }
    }

    /// <summary>
    /// Information about a local image file
    /// </summary>
    public class LocalImageInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public double FileSizeInMB { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public bool IsValidImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
} 