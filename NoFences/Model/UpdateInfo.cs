using System;
using System.Collections.Generic;

namespace NoFences.Model
{
    /// <summary>
    /// Contains information about an available software update from GitHub Releases API.
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// Gets or sets the version number of the available update (e.g., "1.7.0.0").
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or sets the release date and time (UTC).
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the direct download URL for the installer.
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to the release notes page on GitHub.
        /// </summary>
        public string ReleaseNotesUrl { get; set; }

        /// <summary>
        /// Gets or sets the file size of the installer in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 hash of the installer file for integrity verification.
        /// </summary>
        public string Sha256Hash { get; set; }

        /// <summary>
        /// Gets or sets whether this is a critical update that should be installed immediately.
        /// </summary>
        public bool IsCritical { get; set; }

        /// <summary>
        /// Gets or sets the changelog items for this release.
        /// </summary>
        public List<string> ChangelogItems { get; set; }

        /// <summary>
        /// Gets or sets the full release notes body (Markdown format from GitHub).
        /// </summary>
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// Gets a formatted file size string (e.g., "15.2 MB").
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                if (FileSize == 0) return "Unknown";

                string[] sizes = { "B", "KB", "MB", "GB" };
                double size = FileSize;
                int order = 0;

                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size = size / 1024;
                }

                return $"{size:0.#} {sizes[order]}";
            }
        }

        public UpdateInfo()
        {
            ChangelogItems = new List<string>();
        }
    }

    /// <summary>
    /// Result of an update check operation.
    /// </summary>
    public enum UpdateCheckResult
    {
        /// <summary>
        /// An update is available and can be downloaded.
        /// </summary>
        UpdateAvailable,

        /// <summary>
        /// The installed version is up to date.
        /// </summary>
        UpToDate,

        /// <summary>
        /// An error occurred while checking for updates (network, parsing, etc.).
        /// </summary>
        Error,

        /// <summary>
        /// The update check was skipped by user preference.
        /// </summary>
        Skipped
    }
}
