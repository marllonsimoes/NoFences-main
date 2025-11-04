using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NoFencesService.Sync
{
    /// <summary>
    /// Sync provider for physical devices (USB drives, external HDDs, network shares).
    /// Implements ICloudSyncProvider for unified interface.
    /// </summary>
    public class DeviceSyncProvider : ICloudSyncProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DeviceSyncProvider));

        private readonly string devicePath;
        private readonly string deviceName;

        public string ProviderName => $"Device:{deviceName}";

        /// <summary>
        /// Create provider for a specific device path
        /// </summary>
        /// <param name="devicePath">Root path of the device (e.g., "E:\", "\\server\share")</param>
        /// <param name="deviceName">User-friendly device name</param>
        public DeviceSyncProvider(string devicePath, string deviceName)
        {
            this.devicePath = devicePath ?? throw new ArgumentNullException(nameof(devicePath));
            this.deviceName = deviceName ?? "Unknown Device";

            log.Info($"DeviceSyncProvider created: {deviceName} at {devicePath}");
        }

        #region Provider Lifecycle

        public Task<bool> InitializeAsync()
        {
            try
            {
                // Check if device path exists and is accessible
                if (Directory.Exists(devicePath))
                {
                    log.Info($"Device initialized: {devicePath}");
                    return Task.FromResult(true);
                }
                else
                {
                    log.Warn($"Device path not accessible: {devicePath}");
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error initializing device provider: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            // For local devices, "authenticated" means accessible
            return Task.FromResult(Directory.Exists(devicePath));
        }

        #endregion

        #region File Operations

        public async Task<bool> UploadFileAsync(string localPath, string remotePath)
        {
            try
            {
                var destinationPath = GetFullDevicePath(remotePath);
                var destinationDir = Path.GetDirectoryName(destinationPath);

                // Ensure destination directory exists
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Copy file
                await Task.Run(() => File.Copy(localPath, destinationPath, overwrite: true));

                log.Debug($"Uploaded file: {localPath} → {destinationPath}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error uploading file to device: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, string localPath)
        {
            try
            {
                var sourcePath = GetFullDevicePath(remotePath);
                var destinationDir = Path.GetDirectoryName(localPath);

                // Ensure destination directory exists
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Copy file
                await Task.Run(() => File.Copy(sourcePath, localPath, overwrite: true));

                log.Debug($"Downloaded file: {sourcePath} → {localPath}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error downloading file from device: {ex.Message}", ex);
                return false;
            }
        }

        public Task<bool> DeleteFileAsync(string remotePath)
        {
            try
            {
                var fullPath = GetFullDevicePath(remotePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    log.Debug($"Deleted file: {fullPath}");
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                log.Error($"Error deleting file from device: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        public Task<List<CloudFileInfo>> ListFilesAsync(string remotePath)
        {
            try
            {
                var fullPath = GetFullDevicePath(remotePath);
                var files = new List<CloudFileInfo>();

                if (!Directory.Exists(fullPath))
                {
                    log.Warn($"Directory not found on device: {fullPath}");
                    return Task.FromResult(files);
                }

                // Get all files recursively
                var fileInfos = new DirectoryInfo(fullPath).GetFiles("*", SearchOption.AllDirectories);

                foreach (var fileInfo in fileInfos)
                {
                    var relativePath = GetRelativePath(fullPath, fileInfo.FullName);

                    files.Add(new CloudFileInfo
                    {
                        Name = fileInfo.Name,
                        Path = relativePath,
                        Size = fileInfo.Length,
                        ModifiedTime = fileInfo.LastWriteTimeUtc,
                        Hash = null, // Will compute on demand
                        IsDirectory = false
                    });
                }

                log.Debug($"Listed {files.Count} files from device: {fullPath}");
                return Task.FromResult(files);
            }
            catch (Exception ex)
            {
                log.Error($"Error listing files from device: {ex.Message}", ex);
                return Task.FromResult(new List<CloudFileInfo>());
            }
        }

        public Task<CloudFileInfo> GetFileInfoAsync(string remotePath)
        {
            try
            {
                var fullPath = GetFullDevicePath(remotePath);

                if (!File.Exists(fullPath))
                {
                    return Task.FromResult<CloudFileInfo>(null);
                }

                var fileInfo = new FileInfo(fullPath);

                var cloudFileInfo = new CloudFileInfo
                {
                    Name = fileInfo.Name,
                    Path = remotePath,
                    Size = fileInfo.Length,
                    ModifiedTime = fileInfo.LastWriteTimeUtc,
                    Hash = ComputeFileHash(fullPath),
                    IsDirectory = false
                };

                return Task.FromResult(cloudFileInfo);
            }
            catch (Exception ex)
            {
                log.Error($"Error getting file info from device: {ex.Message}", ex);
                return Task.FromResult<CloudFileInfo>(null);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get full device path from relative path
        /// </summary>
        private string GetFullDevicePath(string relativePath)
        {
            // Normalize path separators
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

            // Remove leading slash if present
            if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                relativePath = relativePath.Substring(1);
            }

            return Path.Combine(devicePath, relativePath);
        }

        /// <summary>
        /// Get relative path from full path
        /// </summary>
        private string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var fullUri = new Uri(fullPath);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Compute MD5 hash of file
        /// </summary>
        private string ComputeFileHash(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                log.Debug($"Error computing file hash: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Device Detection Helpers

        /// <summary>
        /// Check if device is currently connected and accessible
        /// </summary>
        public bool IsDeviceConnected()
        {
            return Directory.Exists(devicePath);
        }

        /// <summary>
        /// Get device information (volume label, serial number, etc.)
        /// </summary>
        public DeviceInfo GetDeviceInfo()
        {
            try
            {
                if (!Directory.Exists(devicePath))
                    return null;

                var driveInfo = new DriveInfo(Path.GetPathRoot(devicePath));

                return new DeviceInfo
                {
                    DriveLetter = driveInfo.Name,
                    VolumeLabel = driveInfo.VolumeLabel,
                    DriveType = driveInfo.DriveType.ToString(),
                    TotalSize = driveInfo.TotalSize,
                    AvailableSpace = driveInfo.AvailableFreeSpace,
                    IsReady = driveInfo.IsReady
                };
            }
            catch (Exception ex)
            {
                log.Debug($"Error getting device info: {ex.Message}");
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Device information
    /// </summary>
    public class DeviceInfo
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public string DriveType { get; set; }
        public long TotalSize { get; set; }
        public long AvailableSpace { get; set; }
        public bool IsReady { get; set; }
    }
}
