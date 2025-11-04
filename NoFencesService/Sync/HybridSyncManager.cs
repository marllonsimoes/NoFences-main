using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NoFencesService.Sync
{
    /// <summary>
    /// Manages hybrid sync between local files, cloud storage, and physical devices.
    /// Coordinates CloudSyncEngine with device monitoring for complete backup solution.
    /// </summary>
    public class HybridSyncManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(HybridSyncManager));

        private readonly CloudSyncEngine cloudSyncEngine;
        private readonly Dictionary<string, ICloudSyncProvider> deviceProviders;
        private readonly List<MultiTargetSyncConfiguration> syncConfigurations;
        private readonly object syncLock = new object();

        /// <summary>
        /// Event fired when a device becomes available and triggers sync
        /// </summary>
        public event EventHandler<DeviceSyncEventArgs> DeviceSyncTriggered;

        public HybridSyncManager(CloudSyncEngine cloudSyncEngine)
        {
            this.cloudSyncEngine = cloudSyncEngine ?? throw new ArgumentNullException(nameof(cloudSyncEngine));
            this.deviceProviders = new Dictionary<string, ICloudSyncProvider>();
            this.syncConfigurations = new List<MultiTargetSyncConfiguration>();

            log.Info("HybridSyncManager initialized");
        }

        #region Configuration Management

        /// <summary>
        /// Add a multi-target sync configuration
        /// </summary>
        public void AddSyncConfiguration(MultiTargetSyncConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (syncLock)
            {
                syncConfigurations.Add(config);
                log.Info($"Added sync configuration: {config.Name} ({config.Targets.Count} targets)");

                // Register cloud targets with CloudSyncEngine
                foreach (var target in config.Targets.Where(t => t.TargetType == SyncTargetType.Cloud))
                {
                    RegisterCloudTarget(config, target);
                }

                // Create device providers for device targets
                foreach (var target in config.Targets.Where(t => t.TargetType == SyncTargetType.Device))
                {
                    CreateDeviceProvider(target);
                }
            }
        }

        /// <summary>
        /// Get all sync configurations
        /// </summary>
        public List<MultiTargetSyncConfiguration> GetSyncConfigurations()
        {
            lock (syncLock)
            {
                return new List<MultiTargetSyncConfiguration>(syncConfigurations);
            }
        }

        /// <summary>
        /// Remove a sync configuration
        /// </summary>
        public void RemoveSyncConfiguration(Guid configId)
        {
            lock (syncLock)
            {
                var config = syncConfigurations.FirstOrDefault(c => c.Id == configId);
                if (config != null)
                {
                    syncConfigurations.Remove(config);
                    log.Info($"Removed sync configuration: {config.Name}");

                    // TODO: Unregister from CloudSyncEngine
                }
            }
        }

        #endregion

        #region Device Management

        /// <summary>
        /// Handle device connected event
        /// </summary>
        public async Task OnDeviceConnected(string driveLetter, string volumeLabel, string serialNumber)
        {
            log.Info($"Device connected: {driveLetter} (Volume: {volumeLabel})");

            List<MultiTargetSyncConfiguration> matchingConfigs;
            lock (syncLock)
            {
                // Find sync configurations that target this device
                matchingConfigs = syncConfigurations
                    .Where(c => c.Enabled)
                    .Where(c => c.Targets.Any(t =>
                        t.TargetType == SyncTargetType.Device &&
                        t.AutoSync &&
                        MatchesDevice(t, driveLetter, volumeLabel, serialNumber)))
                    .ToList();
            }

            if (matchingConfigs.Count == 0)
            {
                log.Debug($"No sync configurations for device {driveLetter}");
                return;
            }

            log.Info($"Found {matchingConfigs.Count} sync configurations for device {driveLetter}");

            // Trigger sync for each matching configuration
            foreach (var config in matchingConfigs)
            {
                await SyncToDeviceAsync(config, driveLetter, volumeLabel);
            }
        }

        /// <summary>
        /// Handle device disconnected event
        /// </summary>
        public void OnDeviceDisconnected(string driveLetter)
        {
            log.Info($"Device disconnected: {driveLetter}");

            lock (syncLock)
            {
                // Mark device targets as unavailable
                foreach (var config in syncConfigurations)
                {
                    foreach (var target in config.Targets.Where(t => t.TargetType == SyncTargetType.Device))
                    {
                        if (target.Identifier == driveLetter)
                        {
                            target.IsAvailable = false;
                            log.Debug($"Marked device target unavailable: {target.Name}");
                        }
                    }
                }

                // Remove device provider
                if (deviceProviders.ContainsKey(driveLetter))
                {
                    deviceProviders.Remove(driveLetter);
                    log.Debug($"Removed device provider: {driveLetter}");
                }
            }
        }

        /// <summary>
        /// Check if device matches sync target criteria
        /// </summary>
        private bool MatchesDevice(SyncTarget target, string driveLetter, string volumeLabel, string serialNumber)
        {
            // Match by drive letter
            if (!string.IsNullOrEmpty(target.Identifier) && target.Identifier.Equals(driveLetter, StringComparison.OrdinalIgnoreCase))
                return true;

            // Match by volume label
            if (!string.IsNullOrEmpty(target.DeviceVolumeLabel) && target.DeviceVolumeLabel.Equals(volumeLabel, StringComparison.OrdinalIgnoreCase))
                return true;

            // Match by serial number (most reliable)
            if (!string.IsNullOrEmpty(target.DeviceSerialNumber) && target.DeviceSerialNumber.Equals(serialNumber, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        #endregion

        #region Sync Operations

        /// <summary>
        /// Sync to a specific device
        /// </summary>
        private async Task SyncToDeviceAsync(MultiTargetSyncConfiguration config, string driveLetter, string volumeLabel)
        {
            try
            {
                var target = config.Targets.First(t =>
                    t.TargetType == SyncTargetType.Device &&
                    MatchesDevice(t, driveLetter, volumeLabel, null));

                log.Info($"Starting device sync: {config.Name} → {driveLetter}");

                // Get or create device provider
                var provider = GetOrCreateDeviceProvider(driveLetter, target);

                if (!await provider.IsAuthenticatedAsync())
                {
                    log.Warn($"Device not accessible: {driveLetter}");
                    return;
                }

                // Mark target as available
                target.IsAvailable = true;

                // Perform sync based on direction
                bool success = await PerformDeviceSyncAsync(config, target, provider);

                if (success)
                {
                    target.LastSyncTime = DateTime.UtcNow;
                    log.Info($"Device sync completed: {config.Name} → {driveLetter}");

                    DeviceSyncTriggered?.Invoke(this, new DeviceSyncEventArgs
                    {
                        ConfigurationName = config.Name,
                        DriveLetter = driveLetter,
                        VolumeLabel = volumeLabel,
                        Success = true
                    });
                }
                else
                {
                    log.Warn($"Device sync failed: {config.Name} → {driveLetter}");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error syncing to device: {ex.Message}", ex);

                DeviceSyncTriggered?.Invoke(this, new DeviceSyncEventArgs
                {
                    ConfigurationName = config.Name,
                    DriveLetter = driveLetter,
                    VolumeLabel = volumeLabel,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Perform actual device sync operation
        /// </summary>
        private async Task<bool> PerformDeviceSyncAsync(MultiTargetSyncConfiguration config, SyncTarget target, ICloudSyncProvider provider)
        {
            log.Debug($"Performing device sync: {config.Direction}");

            // Get source files
            var sourceFiles = GetLocalFiles(config.SourcePath, config.IncludePatterns, config.ExcludePatterns);
            log.Debug($"Found {sourceFiles.Count} source files");

            // Get destination files
            var destinationFiles = await provider.ListFilesAsync(target.Path);
            log.Debug($"Found {destinationFiles.Count} destination files");

            int uploadedCount = 0;
            int downloadedCount = 0;
            int skippedCount = 0;

            switch (config.Direction)
            {
                case SyncDirection.Upload:
                    // Copy source → device
                    foreach (var file in sourceFiles)
                    {
                        var relativePath = GetRelativePath(config.SourcePath, file.FullName);
                        var remotePath = Path.Combine(target.Path, relativePath);

                        var destFile = destinationFiles.FirstOrDefault(f => f.Path == relativePath);
                        if (destFile == null || file.LastWriteTimeUtc > destFile.ModifiedTime)
                        {
                            if (await provider.UploadFileAsync(file.FullName, remotePath))
                            {
                                uploadedCount++;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    break;

                case SyncDirection.Download:
                    // Copy device → source
                    foreach (var file in destinationFiles)
                    {
                        var localPath = Path.Combine(config.SourcePath, file.Path);
                        var localFile = new FileInfo(localPath);

                        if (!localFile.Exists || file.ModifiedTime > localFile.LastWriteTimeUtc)
                        {
                            var remotePath = Path.Combine(target.Path, file.Path);
                            if (await provider.DownloadFileAsync(remotePath, localPath))
                            {
                                downloadedCount++;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    break;

                case SyncDirection.Bidirectional:
                    // Two-way sync (most recent wins)
                    // TODO: Implement conflict resolution
                    log.Warn("Bidirectional device sync not yet fully implemented");
                    break;
            }

            log.Info($"Device sync stats - Uploaded: {uploadedCount}, Downloaded: {downloadedCount}, Skipped: {skippedCount}");
            return true;
        }

        /// <summary>
        /// Manually trigger sync for all enabled configurations
        /// </summary>
        public async Task SyncAllAsync()
        {
            List<MultiTargetSyncConfiguration> enabledConfigs;
            lock (syncLock)
            {
                enabledConfigs = syncConfigurations.Where(c => c.Enabled).ToList();
            }

            log.Info($"Syncing {enabledConfigs.Count} configurations");

            foreach (var config in enabledConfigs)
            {
                // Sync to all available targets
                foreach (var target in config.Targets.Where(t => t.IsAvailable || t.TargetType == SyncTargetType.Cloud))
                {
                    if (target.TargetType == SyncTargetType.Cloud)
                    {
                        // Cloud sync handled by CloudSyncEngine
                        // (Already registered via RegisterCloudTarget)
                        log.Debug($"Cloud target will be synced by CloudSyncEngine: {target.Name}");
                    }
                    else if (target.TargetType == SyncTargetType.Device && target.IsAvailable)
                    {
                        var provider = GetOrCreateDeviceProvider(target.Identifier, target);
                        await PerformDeviceSyncAsync(config, target, provider);
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Register cloud target with CloudSyncEngine
        /// </summary>
        private void RegisterCloudTarget(MultiTargetSyncConfiguration config, SyncTarget target)
        {
            var cloudConfig = new SyncConfiguration
            {
                Name = $"{config.Name} ({target.Name})",
                LocalPath = config.SourcePath,
                RemotePath = target.Path,
                ProviderName = target.Identifier,
                Direction = config.Direction,
                IncludePatterns = new List<string>(config.IncludePatterns),
                ExcludePatterns = new List<string>(config.ExcludePatterns),
                DeleteOnSync = config.DeleteOnSync,
                SyncIntervalSeconds = config.CloudSyncIntervalSeconds,
                Enabled = config.Enabled
            };

            cloudSyncEngine.AddSyncConfiguration(cloudConfig);
            log.Debug($"Registered cloud target with CloudSyncEngine: {target.Name}");
        }

        /// <summary>
        /// Get or create device provider
        /// </summary>
        private ICloudSyncProvider GetOrCreateDeviceProvider(string driveLetter, SyncTarget target)
        {
            lock (syncLock)
            {
                if (!deviceProviders.TryGetValue(driveLetter, out var provider))
                {
                    provider = CreateDeviceProvider(target);
                    deviceProviders[driveLetter] = provider;
                }
                return provider;
            }
        }

        /// <summary>
        /// Create device provider
        /// </summary>
        private ICloudSyncProvider CreateDeviceProvider(SyncTarget target)
        {
            var provider = new DeviceSyncProvider(target.Identifier, target.Name);
            cloudSyncEngine.RegisterProvider(provider);
            log.Debug($"Created device provider: {target.Name}");
            return provider;
        }

        /// <summary>
        /// Get local files matching patterns
        /// </summary>
        private List<FileInfo> GetLocalFiles(string path, List<string> includePatterns, List<string> excludePatterns)
        {
            if (!Directory.Exists(path))
                return new List<FileInfo>();

            var allFiles = new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories);

            // TODO: Apply include/exclude pattern filtering
            return allFiles.ToList();
        }

        /// <summary>
        /// Get relative path
        /// </summary>
        private string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var fullUri = new Uri(fullPath);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion
    }

    /// <summary>
    /// Event args for device sync events
    /// </summary>
    public class DeviceSyncEventArgs : EventArgs
    {
        public string ConfigurationName { get; set; }
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
