using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ShopifyLib.Models;

namespace ShopifyLib.Services
{
    /// <summary>
    /// Interface for file operations using GraphQL
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Uploads a file from a file path using GraphQL
        /// </summary>
        /// <param name="filePath">Path to the file to upload</param>
        /// <param name="altText">Optional alt text for the file</param>
        /// <param name="resourceId">Optional resource ID to attach the file to</param>
        /// <param name="resourceType">Optional resource type (e.g., "PRODUCT")</param>
        /// <returns>The file creation response</returns>
        Task<FileCreateResponse> UploadFileAsync(string filePath, string? altText = null, string? resourceId = null, string? resourceType = null);

        /// <summary>
        /// Uploads a file from a stream using GraphQL
        /// </summary>
        /// <param name="stream">The file stream</param>
        /// <param name="fileName">The filename</param>
        /// <param name="contentType">The content type</param>
        /// <param name="altText">Optional alt text for the file</param>
        /// <param name="resourceId">Optional resource ID to attach the file to</param>
        /// <param name="resourceType">Optional resource type</param>
        /// <returns>The file creation response</returns>
        Task<FileCreateResponse> UploadFileAsync(Stream stream, string fileName, string contentType, string? altText = null, string? resourceId = null, string? resourceType = null);

        /// <summary>
        /// Uploads multiple files using GraphQL
        /// </summary>
        /// <param name="files">List of file inputs</param>
        /// <returns>The file creation response</returns>
        Task<FileCreateResponse> UploadFilesAsync(List<FileCreateInput> files);

        /// <summary>
        /// Upload a file from a URL using GraphQL
        /// </summary>
        /// <param name="fileUrl">The URL of the file to upload</param>
        /// <param name="contentType">The content type enum (IMAGE, FILE, VIDEO)</param>
        /// <param name="altText">Optional alt text</param>
        /// <returns>The file creation response</returns>
        Task<FileCreateResponse> UploadFileFromUrlAsync(string fileUrl, string contentType = "FILE", string? altText = null);
    }
} 