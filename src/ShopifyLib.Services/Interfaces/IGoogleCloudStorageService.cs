using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShopifyLib.Services.Interfaces
{
    /// <summary>
    /// Interface for Google Cloud Storage signed URL upload operations
    /// </summary>
    public interface IGoogleCloudStorageService
    {
        /// <summary>
        /// Uploads a file to a Google Cloud Storage signed URL
        /// </summary>
        /// <param name="signedUrl">The signed URL from Google Cloud Storage</param>
        /// <param name="fileBytes">The file bytes to upload</param>
        /// <param name="contentType">The MIME type of the file</param>
        /// <param name="fileName">The filename</param>
        /// <returns>Upload response</returns>
        Task<HttpResponseMessage> UploadToSignedUrlAsync(string signedUrl, byte[] fileBytes, string contentType, string fileName);

        /// <summary>
        /// Uploads a file from stream to a Google Cloud Storage signed URL
        /// </summary>
        /// <param name="signedUrl">The signed URL from Google Cloud Storage</param>
        /// <param name="stream">The file stream</param>
        /// <param name="contentType">The MIME type of the file</param>
        /// <param name="fileName">The filename</param>
        /// <returns>Upload response</returns>
        Task<HttpResponseMessage> UploadToSignedUrlAsync(string signedUrl, Stream stream, string contentType, string fileName);
    }
} 