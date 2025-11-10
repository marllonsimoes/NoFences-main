using System;
using System.Collections.Generic;

namespace NoFencesService.Sync
{
    /// <summary>
    /// Configuration for cloud sync operations
    /// </summary>
    public class SyncConfiguration
    {
        /// <summary>
        /// Unique identifier for this sync configuration
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User-friendly name for this sync configuration
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Local directory to sync
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// Remote path in cloud storage
        /// </summary>
        public string RemotePath { get; set; }

        /// <summary>
        /// Cloud provider name (OneDrive, GoogleDrive, Dropbox, etc.)
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        public SyncDirection Direction { get; set; }

        /// <summary>
        /// Whether sync is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Sync interval in seconds (0 = manual only)
        /// </summary>
        public int SyncIntervalSeconds { get; set; }

        /// <summary>
        /// File patterns to include (e.g., "*.jpg", "*.png")
        /// Empty list = all files
        /// </summary>
        public List<string> IncludePatterns { get; set; }

        /// <summary>
        /// File patterns to exclude (e.g., "*.tmp", "*.log")
        /// </summary>
        public List<string> ExcludePatterns { get; set; }

        /// <summary>
        /// Whether to delete files from destination when deleted from source
        /// </summary>
        public bool DeleteOnSync { get; set; }

        /// <summary>
        /// Last successful sync time
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// Last sync status
        /// </summary>
        public SyncStatus LastSyncStatus { get; set; }

        /// <summary>
        /// Last sync error message (if any)
        /// </summary>
        public string LastSyncError { get; set; }

        public SyncConfiguration()
        {
            Id = Guid.NewGuid();
            IncludePatterns = new List<string>();
            ExcludePatterns = new List<string>();
            Direction = SyncDirection.Upload;
            Enabled = true;
            SyncIntervalSeconds = 3600; // Default: 1 hour
            DeleteOnSync = false;
        }
    }

    /// <summary>
    /// Sync direction
    /// </summary>
    public enum SyncDirection
    {
        /// <summary>
        /// Upload local files to cloud
        /// </summary>
        Upload,

        /// <summary>
        /// Download cloud files to local
        /// </summary>
        Download,

        /// <summary>
        /// Two-way sync (most recent wins)
        /// </summary>
        Bidirectional
    }

    /// <summary>
    /// Sync status
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>
        /// Never synced
        /// </summary>
        NeverSynced,

        /// <summary>
        /// Sync in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Sync completed successfully
        /// </summary>
        Success,

        /// <summary>
        /// Sync failed with error
        /// </summary>
        Failed,

        /// <summary>
        /// Sync completed with warnings
        /// </summary>
        PartialSuccess
    }
}
