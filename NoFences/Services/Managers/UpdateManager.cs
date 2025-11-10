using log4net;
using NoFences.Core.Settings;
using NoFences.Model;
using NoFences.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace NoFences.Services.Managers
{
    /// <summary>
    /// Manages software update checking and installation via GitHub Releases API.
    /// Implements IApplicationService for lifecycle management.
    /// </summary>
    public class UpdateManager : IApplicationService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UpdateManager));

        private readonly string githubApiUrl;
        private readonly Version currentVersion;
        private bool isCheckingForUpdates;
        private System.Timers.Timer autoCheckTimer;
        private UserPreferences preferences;
        private NotifyIcon trayIcon; // For balloon notifications

        // GitHub API endpoint format: https://api.github.com/repos/{owner}/{repo}/releases/latest
        private const string DEFAULT_GITHUB_API = "https://api.github.com/marllonsimoes/NoFences-main/releases/latest";

        /// <summary>
        /// Initializes a new instance of the UpdateManager.
        /// </summary>
        /// <param name="apiUrl">Optional custom GitHub API URL. If null, uses default.</param>
        /// <param name="trayIcon">Optional NotifyIcon for balloon notifications.</param>
        public UpdateManager(string apiUrl = null, NotifyIcon trayIcon = null)
        {
            githubApiUrl = apiUrl ?? DEFAULT_GITHUB_API;
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            this.trayIcon = trayIcon;

            log.Info($"UpdateManager initialized with current version: {currentVersion}");
            log.Debug($"GitHub API URL: {githubApiUrl}");

            // Load user preferences
            preferences = UserPreferences.Load();
        }

        /// <summary>
        /// Checks for available updates asynchronously.
        /// </summary>
        /// <returns>Tuple containing the check result and update info (if available).</returns>
        public async Task<(UpdateCheckResult Result, UpdateInfo Update)> CheckForUpdatesAsync()
        {
            if (isCheckingForUpdates)
            {
                log.Warn("Update check already in progress, skipping duplicate request");
                return (UpdateCheckResult.Skipped, null);
            }

            isCheckingForUpdates = true;

            try
            {
                log.Info("Checking for software updates...");

                // Fetch latest release info from GitHub API
                string jsonResponse = await FetchLatestReleaseAsync();

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    log.Error("Failed to fetch release information from GitHub");
                    return (UpdateCheckResult.Error, null);
                }

                // Parse JSON response into UpdateInfo
                UpdateInfo updateInfo = ParseGitHubRelease(jsonResponse);

                if (updateInfo == null)
                {
                    log.Error("Failed to parse GitHub release JSON");
                    return (UpdateCheckResult.Error, null);
                }

                // Compare versions
                if (updateInfo.Version > currentVersion)
                {
                    log.Info($"Update available: {updateInfo.Version} (current: {currentVersion})");
                    return (UpdateCheckResult.UpdateAvailable, updateInfo);
                }
                else
                {
                    log.Info($"Software is up to date (current: {currentVersion}, latest: {updateInfo.Version})");
                    return (UpdateCheckResult.UpToDate, null);
                }
            }
            catch (WebException webEx)
            {
                log.Error($"Network error while checking for updates: {webEx.Message}", webEx);
                return (UpdateCheckResult.Error, null);
            }
            catch (Exception ex)
            {
                log.Error($"Unexpected error while checking for updates: {ex.Message}", ex);
                return (UpdateCheckResult.Error, null);
            }
            finally
            {
                isCheckingForUpdates = false;
            }
        }

        /// <summary>
        /// Fetches the latest release information from GitHub API.
        /// </summary>
        private async Task<string> FetchLatestReleaseAsync()
        {
            using (var client = new WebClient())
            {
                // GitHub API requires User-Agent header
                client.Headers.Add("User-Agent", $"NoFences/{currentVersion}");

                try
                {
                    log.Debug($"Fetching release info from: {githubApiUrl}");
                    string json = await client.DownloadStringTaskAsync(githubApiUrl);
                    log.Debug($"Successfully fetched {json.Length} bytes of JSON");
                    return json;
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse response)
                    {
                        log.Error($"HTTP error {(int)response.StatusCode}: {response.StatusDescription}");
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Parses GitHub Releases API JSON response into UpdateInfo.
        /// Uses simple regex parsing to avoid external JSON library dependency.
        /// </summary>
        private UpdateInfo ParseGitHubRelease(string json)
        {
            try
            {
                var updateInfo = new UpdateInfo();

                // Extract tag_name (version)
                var tagMatch = Regex.Match(json, @"""tag_name""\s*:\s*""v?([0-9.]+)""");
                if (tagMatch.Success)
                {
                    string versionString = tagMatch.Groups[1].Value;
                    updateInfo.Version = new Version(versionString);
                    log.Debug($"Parsed version: {updateInfo.Version}");
                }
                else
                {
                    log.Error("Could not find 'tag_name' in GitHub release JSON");
                    return null;
                }

                // Extract published_at (release date)
                var dateMatch = Regex.Match(json, @"""published_at""\s*:\s*""([^""]+)""");
                if (dateMatch.Success)
                {
                    if (DateTime.TryParse(dateMatch.Groups[1].Value, out DateTime releaseDate))
                    {
                        updateInfo.ReleaseDate = releaseDate.ToUniversalTime();
                        log.Debug($"Parsed release date: {updateInfo.ReleaseDate}");
                    }
                }

                // Extract html_url (release notes page)
                var urlMatch = Regex.Match(json, @"""html_url""\s*:\s*""([^""]+)""");
                if (urlMatch.Success)
                {
                    updateInfo.ReleaseNotesUrl = urlMatch.Groups[1].Value;
                    log.Debug($"Release notes URL: {updateInfo.ReleaseNotesUrl}");
                }

                // Extract body (release notes markdown)
                var bodyMatch = Regex.Match(json, @"""body""\s*:\s*""((?:[^""\\]|\\.)*)""");
                if (bodyMatch.Success)
                {
                    // Unescape JSON string
                    string body = bodyMatch.Groups[1].Value;
                    body = Regex.Unescape(body);
                    updateInfo.ReleaseNotes = body;
                    log.Debug($"Parsed release notes: {body.Length} characters");
                }

                // Extract assets (find .exe installer)
                var assetsMatch = Regex.Match(json, @"""assets""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
                if (assetsMatch.Success)
                {
                    string assetsJson = assetsMatch.Groups[1].Value;

                    // Look for .exe file in assets
                    var exeMatch = Regex.Match(assetsJson, @"""browser_download_url""\s*:\s*""([^""]*\.exe)""");
                    if (exeMatch.Success)
                    {
                        updateInfo.DownloadUrl = exeMatch.Groups[1].Value;
                        log.Debug($"Found installer download URL: {updateInfo.DownloadUrl}");
                    }

                    // Extract file size
                    var sizeMatch = Regex.Match(assetsJson, @"""size""\s*:\s*(\d+)");
                    if (sizeMatch.Success && long.TryParse(sizeMatch.Groups[1].Value, out long fileSize))
                    {
                        updateInfo.FileSize = fileSize;
                        log.Debug($"Installer file size: {updateInfo.FormattedFileSize}");
                    }
                }

                // Validate we have the minimum required info
                if (updateInfo.Version == null || string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    log.Error("GitHub release missing required fields (version or download URL)");
                    return null;
                }

                return updateInfo;
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing GitHub release JSON: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Shows update notification to the user via modern WPF window.
        /// Displays version info, release notes, and download option.
        /// </summary>
        public void ShowUpdateNotification(UpdateInfo updateInfo)
        {
            if (updateInfo == null)
            {
                throw new ArgumentNullException(nameof(updateInfo));
            }

            try
            {
                log.Info($"Showing update notification window for version {updateInfo.Version}");

                // Must be invoked on UI thread for WPF window
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var updateWindow = new NoFences.View.UpdateNotificationWindow(updateInfo, currentVersion, this);
                    updateWindow.ShowDialog();

                    if (updateWindow.UserWantsToDownload)
                    {
                        log.Info("User accepted update download from WPF window");
                    }
                    else
                    {
                        log.Info("User dismissed update notification");
                    }
                });
            }
            catch (Exception ex)
            {
                log.Error($"Failed to show WPF update window, falling back to MessageBox: {ex.Message}", ex);

                // Fallback to MessageBox if WPF window fails
                string message = $"A new version of NoFences is available!\n\n" +
                               $"Current Version: {currentVersion}\n" +
                               $"New Version: {updateInfo.Version}\n" +
                               $"File Size: {updateInfo.FormattedFileSize}\n\n" +
                               $"Would you like to download the update?";

                var result = MessageBox.Show(message, "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    OpenReleasePageInBrowser(updateInfo);
                }
            }
        }

        /// <summary>
        /// Shows "up to date" message to the user.
        /// </summary>
        public void ShowUpToDateMessage()
        {
            string message = $"You are running the latest version of NoFences.\n\n" +
                           $"Current Version: {currentVersion}";

            MessageBox.Show(message, "No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows error message when update check fails.
        /// </summary>
        public void ShowUpdateCheckError()
        {
            string message = $"Failed to check for updates.\n\n" +
                           $"Please check your internet connection and try again later.\n" +
                           $"You can also visit the releases page manually.";

            var result = MessageBox.Show(message, "Update Check Failed", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

            if (result == DialogResult.OK)
            {
                // Open releases page
                OpenReleasePageInBrowser(new UpdateInfo { ReleaseNotesUrl = "https://github.com/marllonsimoes/NoFences-main/releases" });
            }
        }

        /// <summary>
        /// Opens the GitHub releases page in the default browser.
        /// </summary>
        private void OpenReleasePageInBrowser(UpdateInfo updateInfo)
        {
            if (updateInfo == null || string.IsNullOrEmpty(updateInfo.ReleaseNotesUrl))
            {
                log.Warn("No release notes URL available, using default releases page");
                updateInfo = new UpdateInfo { ReleaseNotesUrl = "https://github.com/marllonsimoes/NoFences-main/releases" };
            }

            try
            {
                log.Info($"Opening release page in browser: {updateInfo.ReleaseNotesUrl}");
                Process.Start(new ProcessStartInfo
                {
                    FileName = updateInfo.ReleaseNotesUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open browser: {ex.Message}", ex);
                MessageBox.Show($"Could not open browser.\n\nPlease visit:\n{updateInfo.ReleaseNotesUrl}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region IApplicationService Implementation

        public void Start()
        {
            log.Info("UpdateManager service started");

            // Phase 3: Start background auto-check timer if enabled
            if (preferences.AutoCheckForUpdates)
            {
                StartAutoCheckTimer();

                // Check immediately if last check was more than CheckFrequencyHours ago
                var timeSinceLastCheck = DateTime.UtcNow - preferences.LastUpdateCheck;
                if (timeSinceLastCheck.TotalHours >= preferences.CheckFrequencyHours)
                {
                    log.Info("Performing initial update check on startup");
                    Task.Run(async () => await PerformBackgroundUpdateCheck());
                }
                else
                {
                    log.Debug($"Skipping initial check, last check was {timeSinceLastCheck.TotalHours:F1} hours ago");
                }
            }
            else
            {
                log.Info("Auto-update checking is disabled in preferences");
            }
        }

        public void Stop()
        {
            log.Info("UpdateManager service stopped");
            StopAutoCheckTimer();
        }

        #endregion

        #region Phase 3: Background Auto-Check Timer

        /// <summary>
        /// Starts the background timer for automatic update checking.
        /// </summary>
        private void StartAutoCheckTimer()
        {
            if (autoCheckTimer != null)
            {
                log.Warn("Auto-check timer already running");
                return;
            }

            // Convert hours to milliseconds
            double intervalMs = preferences.CheckFrequencyHours * 60 * 60 * 1000;

            autoCheckTimer = new System.Timers.Timer(intervalMs);
            autoCheckTimer.Elapsed += OnAutoCheckTimerElapsed;
            autoCheckTimer.AutoReset = true;
            autoCheckTimer.Start();

            log.Info($"Auto-check timer started with {preferences.CheckFrequencyHours}-hour interval");
        }

        /// <summary>
        /// Stops the background timer.
        /// </summary>
        private void StopAutoCheckTimer()
        {
            if (autoCheckTimer != null)
            {
                autoCheckTimer.Stop();
                autoCheckTimer.Elapsed -= OnAutoCheckTimerElapsed;
                autoCheckTimer.Dispose();
                autoCheckTimer = null;
                log.Info("Auto-check timer stopped");
            }
        }

        /// <summary>
        /// Timer callback for background update checks.
        /// </summary>
        private async void OnAutoCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            log.Debug("Auto-check timer elapsed, performing background update check");
            await PerformBackgroundUpdateCheck();
        }

        /// <summary>
        /// Performs a background update check and shows balloon notification if update available.
        /// Does not show notification if user previously skipped this version.
        /// </summary>
        private async Task PerformBackgroundUpdateCheck()
        {
            try
            {
                log.Info("Background update check started");

                var (result, updateInfo) = await CheckForUpdatesAsync();

                // Update last check time
                preferences.LastUpdateCheck = DateTime.UtcNow;
                preferences.Save();

                switch (result)
                {
                    case UpdateCheckResult.UpdateAvailable:
                        // Don't notify if user previously skipped this version
                        if (preferences.LastSkippedVersion == updateInfo.Version.ToString())
                        {
                            log.Info($"Update {updateInfo.Version} available but user previously skipped it");
                            return;
                        }

                        log.Info($"Background check found update: {updateInfo.Version}");
                        ShowBalloonNotification(updateInfo);
                        break;

                    case UpdateCheckResult.UpToDate:
                        log.Debug("Background check: software is up to date");
                        break;

                    case UpdateCheckResult.Error:
                        log.Warn("Background check failed (network or parsing error)");
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Background update check failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows a balloon notification in the system tray when an update is available.
        /// </summary>
        private void ShowBalloonNotification(UpdateInfo updateInfo)
        {
            if (trayIcon == null)
            {
                log.Warn("Cannot show balloon notification: tray icon not available");
                return;
            }

            try
            {
                string title = "NoFences Update Available";
                string message = $"Version {updateInfo.Version} is ready to download.\n" +
                               $"Click to view details.";

                // Store updateInfo for balloon click handler
                trayIcon.Tag = updateInfo;

                // Show balloon tip
                trayIcon.ShowBalloonTip(
                    10000, // 10 seconds
                    title,
                    message,
                    ToolTipIcon.Info
                );

                // Handle balloon click
                trayIcon.BalloonTipClicked += OnBalloonTipClicked;

                log.Info($"Balloon notification shown for version {updateInfo.Version}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to show balloon notification: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles balloon tip click - shows full update notification window.
        /// </summary>
        private void OnBalloonTipClicked(object sender, EventArgs e)
        {
            try
            {
                var updateInfo = trayIcon?.Tag as UpdateInfo;
                if (updateInfo != null)
                {
                    log.Info("User clicked balloon notification, showing update window");
                    ShowUpdateNotification(updateInfo);
                }

                // Unsubscribe after single use
                trayIcon.BalloonTipClicked -= OnBalloonTipClicked;
            }
            catch (Exception ex)
            {
                log.Error($"Error handling balloon tip click: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Marks a version as skipped so user won't be notified again.
        /// </summary>
        public void SkipVersion(Version version)
        {
            preferences.LastSkippedVersion = version.ToString();
            preferences.Save();
            log.Info($"Version {version} marked as skipped");
        }

        #endregion

        #region Phase 4: Auto-Download with Progress

        /// <summary>
        /// Downloads the update installer to a temporary location with progress reporting.
        /// </summary>
        /// <param name="updateInfo">Update information including download URL.</param>
        /// <param name="progressCallback">Optional callback for progress updates (0-100).</param>
        /// <returns>Path to downloaded file, or null if download failed.</returns>
        public async Task<string> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<int> progressCallback = null)
        {
            if (updateInfo == null || string.IsNullOrEmpty(updateInfo.DownloadUrl))
            {
                throw new ArgumentException("Invalid update info or download URL");
            }

            string tempFilePath = null;

            try
            {
                // Create temp file path
                string tempDir = Path.GetTempPath();
                string fileName = $"NoFences-{updateInfo.Version}-Setup.exe";
                tempFilePath = Path.Combine(tempDir, fileName);

                log.Info($"Starting download: {updateInfo.DownloadUrl}");
                log.Info($"Downloading to: {tempFilePath}");

                using (var client = new WebClient())
                {
                    // Add user agent
                    client.Headers.Add("User-Agent", $"NoFences/{currentVersion}");

                    // Progress reporting
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        progressCallback?.Report(e.ProgressPercentage);
                    };

                    // Download file
                    await client.DownloadFileTaskAsync(updateInfo.DownloadUrl, tempFilePath);
                }

                log.Info($"Download completed: {tempFilePath}");

                // Verify file exists
                if (!File.Exists(tempFilePath))
                {
                    log.Error("Downloaded file does not exist");
                    return null;
                }

                // Verify file size matches (if available)
                var fileInfo = new FileInfo(tempFilePath);
                if (updateInfo.FileSize > 0 && fileInfo.Length != updateInfo.FileSize)
                {
                    log.Warn($"File size mismatch: expected {updateInfo.FileSize}, got {fileInfo.Length}");
                }
                else
                {
                    log.Info($"File size verified: {fileInfo.Length} bytes");
                }

                return tempFilePath;
            }
            catch (WebException webEx)
            {
                log.Error($"Network error during download: {webEx.Message}", webEx);

                // Clean up partial download
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }

                return null;
            }
            catch (Exception ex)
            {
                log.Error($"Error downloading update: {ex.Message}", ex);

                // Clean up partial download
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }

                return null;
            }
        }

        /// <summary>
        /// Verifies the SHA-256 hash of a downloaded file.
        /// </summary>
        /// <param name="filePath">Path to file to verify.</param>
        /// <param name="expectedHash">Expected SHA-256 hash (hex string).</param>
        /// <returns>True if hash matches, false otherwise.</returns>
        public bool VerifyFileHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                log.Error("Cannot verify hash: file does not exist");
                return false;
            }

            if (string.IsNullOrEmpty(expectedHash))
            {
                log.Warn("No expected hash provided, skipping verification");
                return true; // Consider valid if no hash to verify
            }

            try
            {
                log.Info($"Verifying SHA-256 hash for: {filePath}");

                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    string actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    bool matches = actualHash.Equals(expectedHash.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);

                    if (matches)
                    {
                        log.Info("SHA-256 hash verification: PASSED");
                    }
                    else
                    {
                        log.Error($"SHA-256 hash verification: FAILED");
                        log.Error($"  Expected: {expectedHash}");
                        log.Error($"  Actual:   {actualHash}");
                    }

                    return matches;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error verifying file hash: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Launches the installer and optionally exits the application.
        /// </summary>
        /// <param name="installerPath">Path to installer executable.</param>
        /// <param name="exitApplication">If true, exits NoFences after launching installer.</param>
        /// <returns>True if installer launched successfully.</returns>
        public bool LaunchInstaller(string installerPath, bool exitApplication = true)
        {
            if (string.IsNullOrEmpty(installerPath) || !File.Exists(installerPath))
            {
                log.Error($"Cannot launch installer: file not found at {installerPath}");
                return false;
            }

            try
            {
                log.Info($"Launching installer: {installerPath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    Verb = "runas" // Request admin elevation
                };

                Process.Start(startInfo);

                log.Info("Installer launched successfully");

                if (exitApplication)
                {
                    log.Info("Exiting application for installer to complete");

                    // Give installer time to start
                    System.Threading.Thread.Sleep(1000);

                    // Exit application
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Application.Current.Shutdown();
                    });

                    Application.Exit();
                }

                return true;
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                // User cancelled UAC prompt
                if (win32Ex.NativeErrorCode == 1223)
                {
                    log.Warn("User cancelled installer elevation prompt");
                    MessageBox.Show(
                        "Installation cancelled. The installer requires administrator privileges.",
                        "NoFences - Installation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    log.Error($"Win32 error launching installer: {win32Ex.Message}", win32Ex);
                    MessageBox.Show(
                        $"Failed to launch installer: {win32Ex.Message}",
                        "NoFences - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return false;
            }
            catch (Exception ex)
            {
                log.Error($"Error launching installer: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to launch installer: {ex.Message}",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        #endregion
    }
}
