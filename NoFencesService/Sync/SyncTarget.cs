using System;
using System.Collections.Generic;

namespace NoFencesService.Sync
{
    /// <summary>
    /// Represents a sync destination - either cloud storage or physical device
    /// </summary>
    public class SyncTarget
    {
        /// <summary>
        /// Unique identifier for this target
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User-friendly name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Target type (Cloud or Device)
        /// </summary>
        public SyncTargetType TargetType { get; set; }

        /// <summary>
        /// For Cloud: Provider name (OneDrive, GoogleDrive, Dropbox)
        /// For Device: Drive letter or device ID
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Remote path in cloud storage or local path on device
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Priority (lower number = higher priority)
        /// Used when multiple targets are available
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether to sync automatically when target becomes available
        /// </summary>
        public bool AutoSync { get; set; }

        /// <summary>
        /// For Device targets: Device volume label for identification
        /// </summary>
        public string DeviceVolumeLabel { get; set; }

        /// <summary>
        /// For Device targets: Device serial number for identification
        /// </summary>
        public string DeviceSerialNumber { get; set; }

        /// <summary>
        /// Whether this target is currently available
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Last successful sync time
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        public SyncTarget()
        {
            Id = Guid.NewGuid();
            AutoSync = true;
            Priority = 100;
        }
    }

    /// <summary>
    /// Type of sync target
    /// </summary>
    public enum SyncTargetType
    {
        /// <summary>
        /// Cloud storage provider (OneDrive, Google Drive, Dropbox, etc.)
        /// Always available when internet is connected
        /// </summary>
        Cloud,

        /// <summary>
        /// Physical device (USB drive, external HDD, network share)
        /// Available only when device is connected/accessible
        /// </summary>
        Device
    }

    /// <summary>
    /// Enhanced sync configuration with multiple targets support
    /// </summary>
    public class MultiTargetSyncConfiguration
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User-friendly name for this sync rule
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Local source path to sync from
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// List of sync targets (cloud and/or devices)
        /// </summary>
        public List<SyncTarget> Targets { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        public SyncDirection Direction { get; set; }

        /// <summary>
        /// File patterns to include
        /// </summary>
        public List<string> IncludePatterns { get; set; }

        /// <summary>
        /// File patterns to exclude
        /// </summary>
        public List<string> ExcludePatterns { get; set; }

        /// <summary>
        /// Whether to delete files that are removed from source
        /// </summary>
        public bool DeleteOnSync { get; set; }

        /// <summary>
        /// Sync interval for cloud targets (in seconds, 0 = manual only)
        /// </summary>
        public int CloudSyncIntervalSeconds { get; set; }

        /// <summary>
        /// Whether this configuration is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Sync strategy when multiple targets are available
        /// </summary>
        public MultiTargetSyncStrategy SyncStrategy { get; set; }

        public MultiTargetSyncConfiguration()
        {
            Id = Guid.NewGuid();
            Targets = new List<SyncTarget>();
            IncludePatterns = new List<string>();
            ExcludePatterns = new List<string>();
            Direction = SyncDirection.Bidirectional;
            Enabled = true;
            CloudSyncIntervalSeconds = 3600; // 1 hour default
            SyncStrategy = MultiTargetSyncStrategy.AllTargets;
        }
    }

    /// <summary>
    /// Strategy for syncing to multiple targets
    /// </summary>
    public enum MultiTargetSyncStrategy
    {
        /// <summary>
        /// Sync to all available targets
        /// </summary>
        AllTargets,

        /// <summary>
        /// Sync to highest priority available target only
        /// </summary>
        HighestPriorityOnly,

        /// <summary>
        /// Sync to cloud if available, otherwise device
        /// </summary>
        CloudPreferred,

        /// <summary>
        /// Sync to device if available, otherwise cloud
        /// </summary>
        DevicePreferred
    }
}
