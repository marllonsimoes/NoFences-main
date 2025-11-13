using log4net;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using NoFences.Model;
using NoFences.Services.Managers;
using System;
using System.Diagnostics;
using System.Windows;

namespace NoFences.View
{
    /// <summary>
    /// Modern WPF window for displaying software update notifications.
    /// Replaces basic MessageBox with rich UI showing version info and release notes.
    /// Uses MahApps.Metro for automatic dark/light theme based on Windows settings.
    /// </summary>
    public partial class UpdateNotificationWindow : MetroWindow
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UpdateNotificationWindow));
        private readonly UpdateInfo updateInfo;
        private readonly UpdateManager updateManager;

        /// <summary>
        /// Gets whether the user chose to download the update.
        /// </summary>
        public bool UserWantsToDownload { get; private set; }

        public UpdateNotificationWindow(UpdateInfo updateInfo, Version currentVersion, UpdateManager updateManager = null)
        {
            if (updateInfo == null)
            {
                throw new ArgumentNullException(nameof(updateInfo));
            }

            InitializeComponent();

            // Apply MahApps.Metro theme sync with Windows dark/light mode
            ControlzEx.Theming.ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ControlzEx.Theming.ThemeManager.Current.SyncTheme();

            this.updateInfo = updateInfo;
            this.updateManager = updateManager;
            UserWantsToDownload = false;

            // Populate version information
            CurrentVersionText.Text = currentVersion.ToString();
            NewVersionText.Text = updateInfo.Version.ToString();

            // Populate release information
            if (updateInfo.ReleaseDate != default(DateTime))
            {
                // Use DD/MM/YYYY format for international compatibility
                // or culture-aware format based on user's locale
                var localDate = updateInfo.ReleaseDate.ToLocalTime();
                ReleaseDateText.Text = $"Released: {localDate:dd/MM/yyyy}";
            }
            else
            {
                ReleaseDateText.Text = "Release date: Unknown";
            }

            FileSizeText.Text = $"Size: {updateInfo.FormattedFileSize}";

            // Populate release notes
            if (!string.IsNullOrEmpty(updateInfo.ReleaseNotes))
            {
                ReleaseNotesText.Text = updateInfo.ReleaseNotes;
            }
            else if (!string.IsNullOrEmpty(updateInfo.ReleaseNotesUrl))
            {
                ReleaseNotesText.Text = $"Release notes available at:\n{updateInfo.ReleaseNotesUrl}";
            }
            else
            {
                ReleaseNotesText.Text = "No release notes available.";
            }

            // Show critical warning if applicable
            if (updateInfo.IsCritical)
            {
                CriticalWarningPanel.Visibility = Visibility.Visible;
                SubtitleText.Text = "This is a critical security update - installation recommended";
                log.Warn($"Critical update available: {updateInfo.Version}");
            }

            log.Info($"Update notification window opened for version {updateInfo.Version}");
        }

        /// <summary>
        /// User clicked "Download Update" - shows download progress window if UpdateManager available,
        /// otherwise opens browser to release page.
        /// </summary>
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UserWantsToDownload = true;

                // If UpdateManager is available, show download progress window
                if (updateManager != null)
                {
                    log.Info("User chose to download update, showing download progress window");

                    var downloadWindow = new DownloadProgressWindow(updateInfo, updateManager);
                    bool? result = downloadWindow.ShowDialog();

                    if (result == true)
                    {
                        log.Info("Download completed successfully");
                        this.DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        log.Info("Download was cancelled or failed");
                        // Keep update notification window open so user can try again
                    }
                }
                else
                {
                    // Fallback: Open browser to release page
                    string url = !string.IsNullOrEmpty(updateInfo.ReleaseNotesUrl)
                        ? updateInfo.ReleaseNotesUrl
                        : "https://github.com/marllonsimoes/NoFences-main/releases";

                    log.Info($"UpdateManager not available, opening browser: {url}");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to start download: {ex.Message}", ex);
                MessageBox.Show(
                    $"Could not start download.\n\nError: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// User clicked "Remind Me Later" - close window without downloading.
        /// </summary>
        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            log.Info("User chose to skip update for now");
            UserWantsToDownload = false;
            this.DialogResult = false;
            this.Close();
        }
    }
}
