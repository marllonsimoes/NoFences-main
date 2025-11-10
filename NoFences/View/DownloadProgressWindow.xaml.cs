using log4net;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using NoFences.Model;
using NoFences.Services.Managers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NoFences.View
{
    /// <summary>
    /// Progress window for downloading software updates with real-time feedback.
    /// Shows download progress, speed, time remaining, and status messages.
    /// Uses MahApps.Metro for automatic dark/light theme based on Windows settings.
    /// </summary>
    public partial class DownloadProgressWindow : MetroWindow
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DownloadProgressWindow));

        private readonly UpdateInfo updateInfo;
        private readonly UpdateManager updateManager;
        private CancellationTokenSource cancellationTokenSource;
        private bool downloadCancelled;
        private bool downloadCompleted;
        private string downloadedFilePath;

        // Progress tracking
        private Stopwatch downloadTimer;
        private long lastBytesReceived;
        private DateTime lastSpeedUpdate;

        /// <summary>
        /// Gets the path to the downloaded file (null if download failed or cancelled).
        /// </summary>
        public string DownloadedFilePath => downloadedFilePath;

        public DownloadProgressWindow(UpdateInfo updateInfo, UpdateManager updateManager)
        {
            if (updateInfo == null)
            {
                throw new ArgumentNullException(nameof(updateInfo));
            }

            if (updateManager == null)
            {
                throw new ArgumentNullException(nameof(updateManager));
            }

            InitializeComponent();

            // Apply MahApps.Metro theme sync with Windows dark/light mode
            ControlzEx.Theming.ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ControlzEx.Theming.ThemeManager.Current.SyncTheme();

            this.updateInfo = updateInfo;
            this.updateManager = updateManager;

            cancellationTokenSource = new CancellationTokenSource();
            downloadTimer = new Stopwatch();
            lastSpeedUpdate = DateTime.Now;

            // Set version text
            VersionText.Text = $"Version {updateInfo.Version}";

            // Start download automatically
            Loaded += async (s, e) => await StartDownload();

            log.Info($"Download progress window opened for version {updateInfo.Version}");
        }

        /// <summary>
        /// Starts the download process.
        /// </summary>
        private async Task StartDownload()
        {
            try
            {
                StatusText.Text = "Connecting to download server...";
                downloadTimer.Start();

                // Create progress reporter
                var progress = new Progress<int>(percent =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateProgress(percent);
                    });
                });

                // Start download
                downloadedFilePath = await updateManager.DownloadUpdateAsync(updateInfo, progress);

                downloadTimer.Stop();

                if (downloadedFilePath != null && !downloadCancelled)
                {
                    downloadCompleted = true;
                    StatusText.Text = "Download completed successfully!";
                    ProgressText.Text = "100%";
                    ProgressBar.Value = 100;

                    log.Info($"Download completed: {downloadedFilePath}");

                    // Verify hash if available
                    if (!string.IsNullOrEmpty(updateInfo.Sha256Hash))
                    {
                        StatusText.Text = "Verifying file integrity...";
                        await Task.Delay(500); // Brief pause for UI update

                        bool hashValid = updateManager.VerifyFileHash(downloadedFilePath, updateInfo.Sha256Hash);

                        if (hashValid)
                        {
                            StatusText.Text = "File integrity verified. Ready to install.";
                            log.Info("Hash verification passed");
                            await PromptForInstallation();
                        }
                        else
                        {
                            StatusText.Text = "ERROR: File integrity check failed! Download may be corrupted.";
                            log.Error("Hash verification failed");
                            MessageBox.Show(
                                "The downloaded file failed integrity verification. Please try downloading again.",
                                "NoFences - Download Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            this.DialogResult = false;
                            this.Close();
                        }
                    }
                    else
                    {
                        // No hash to verify, proceed to installation
                        StatusText.Text = "Download complete. Ready to install.";
                        await PromptForInstallation();
                    }
                }
                else if (downloadCancelled)
                {
                    log.Info("Download cancelled by user");
                    StatusText.Text = "Download cancelled.";
                    this.DialogResult = false;
                    this.Close();
                }
                else
                {
                    log.Error("Download failed");
                    StatusText.Text = "Download failed. Please try again later.";
                    MessageBox.Show(
                        "Failed to download update. Please check your internet connection and try again.",
                        "NoFences - Download Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    this.DialogResult = false;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error during download: {ex.Message}", ex);
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"An error occurred during download:\n\n{ex.Message}",
                    "NoFences - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        /// <summary>
        /// Updates the progress UI with current download percentage.
        /// </summary>
        private void UpdateProgress(int percent)
        {
            ProgressBar.Value = percent;
            ProgressText.Text = $"{percent}%";

            // Calculate downloaded bytes
            long totalBytes = updateInfo.FileSize;
            long downloadedBytes = (long)(totalBytes * (percent / 100.0));

            // Update downloaded text
            DownloadedText.Text = $"{FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)}";

            // Calculate speed (update every 500ms to avoid flickering)
            var timeSinceLastUpdate = (DateTime.Now - lastSpeedUpdate).TotalMilliseconds;
            if (timeSinceLastUpdate >= 500)
            {
                long bytesSinceLastUpdate = downloadedBytes - lastBytesReceived;
                double secondsElapsed = timeSinceLastUpdate / 1000.0;
                long bytesPerSecond = (long)(bytesSinceLastUpdate / secondsElapsed);

                SpeedText.Text = $"{FormatBytes(bytesPerSecond)}/s";

                // Calculate time remaining
                long bytesRemaining = totalBytes - downloadedBytes;
                if (bytesPerSecond > 0)
                {
                    double secondsRemaining = bytesRemaining / (double)bytesPerSecond;
                    TimeRemainingText.Text = FormatTimeRemaining(secondsRemaining);
                }

                lastBytesReceived = downloadedBytes;
                lastSpeedUpdate = DateTime.Now;
            }

            // Update status
            if (percent < 100)
            {
                StatusText.Text = $"Downloading update... {percent}% complete";
            }
        }

        /// <summary>
        /// Formats bytes to human-readable format (KB, MB, GB).
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int order = 0;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.#} {sizes[order]}";
        }

        /// <summary>
        /// Formats seconds remaining to human-readable format.
        /// </summary>
        private string FormatTimeRemaining(double seconds)
        {
            if (seconds < 60)
            {
                return $"{(int)seconds} seconds";
            }
            else if (seconds < 3600)
            {
                int minutes = (int)(seconds / 60);
                return $"{minutes} minute{(minutes != 1 ? "s" : "")}";
            }
            else
            {
                int hours = (int)(seconds / 3600);
                int minutes = (int)((seconds % 3600) / 60);
                return $"{hours} hour{(hours != 1 ? "s" : "")} {minutes} minute{(minutes != 1 ? "s" : "")}";
            }
        }

        /// <summary>
        /// Prompts the user to install the update after download completes.
        /// </summary>
        private async Task PromptForInstallation()
        {
            await Task.Delay(1000); // Brief pause to let user see "completed" message

            var result = MessageBox.Show(
                "Update downloaded successfully!\n\n" +
                "Would you like to install the update now?\n\n" +
                "NoFences will close and the installer will launch.",
                "NoFences - Install Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                log.Info("User chose to install update immediately");
                StatusText.Text = "Launching installer...";
                await Task.Delay(500);

                bool launched = updateManager.LaunchInstaller(downloadedFilePath, exitApplication: true);

                if (launched)
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    StatusText.Text = "Failed to launch installer.";
                    this.DialogResult = false;
                }
            }
            else
            {
                log.Info("User chose to install update later");
                MessageBox.Show(
                    $"The update has been downloaded to:\n\n{downloadedFilePath}\n\n" +
                    "You can run the installer manually when ready.",
                    "NoFences - Update Downloaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
        }

        /// <summary>
        /// Cancels the download when user clicks Cancel button.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel the download?",
                "NoFences - Cancel Download",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                log.Info("User cancelled download");
                downloadCancelled = true;
                cancellationTokenSource?.Cancel();
                StatusText.Text = "Cancelling download...";
                CancelButton.IsEnabled = false;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Prevent closing if download is in progress
            if (!downloadCompleted && !downloadCancelled)
            {
                var result = MessageBox.Show(
                    "Download is still in progress. Are you sure you want to cancel?",
                    "NoFences - Cancel Download",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                downloadCancelled = true;
                cancellationTokenSource?.Cancel();
            }

            cancellationTokenSource?.Dispose();
            base.OnClosing(e);
        }
    }
}
