using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoFencesService.Sync
{
    /// <summary>
    /// Interface for cloud storage providers (OneDrive, Google Drive, Dropbox, etc.)
    /// </summary>
    public interface ICloudSyncProvider
    {
        /// <summary>
        /// Name of the provider (e.g., "OneDrive", "Google Drive")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Initialize connection to cloud provider
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Check if provider is authenticated and ready
        /// </summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Upload a file to cloud storage
        /// </summary>
        /// <param name="localPath">Local file path</param>
        /// <param name="remotePath">Remote path in cloud storage</param>
        /// <returns>True if successful</returns>
        Task<bool> UploadFileAsync(string localPath, string remotePath);

        /// <summary>
        /// Download a file from cloud storage
        /// </summary>
        /// <param name="remotePath">Remote path in cloud storage</param>
        /// <param name="localPath">Local destination path</param>
        /// <returns>True if successful</returns>
        Task<bool> DownloadFileAsync(string remotePath, string localPath);

        /// <summary>
        /// Delete a file from cloud storage
        /// </summary>
        /// <param name="remotePath">Remote path in cloud storage</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteFileAsync(string remotePath);

        /// <summary>
        /// List files in a remote directory
        /// </summary>
        /// <param name="remotePath">Remote directory path</param>
        /// <returns>List of file metadata</returns>
        Task<List<CloudFileInfo>> ListFilesAsync(string remotePath);

        /// <summary>
        /// Get file metadata from cloud storage
        /// </summary>
        /// <param name="remotePath">Remote file path</param>
        /// <returns>File metadata or null if not found</returns>
        Task<CloudFileInfo> GetFileInfoAsync(string remotePath);
    }

    /// <summary>
    /// Cloud file metadata
    /// </summary>
    public class CloudFileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string Hash { get; set; }
        public bool IsDirectory { get; set; }
    }
}
