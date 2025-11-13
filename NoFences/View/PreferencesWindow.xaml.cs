using log4net;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using NoFences.Core.Settings;
using NoFences.Services.Managers;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace NoFences.View
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// User settings/preferences dialog for NoFences application.
    /// Uses MahApps.Metro for automatic dark/light theme based on Windows settings.
    /// </summary>
    public partial class PreferencesWindow : MetroWindow
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PreferencesWindow));

        private UserPreferences preferences;
        private UpdateManager updateManager;

        /// <summary>
        /// Initializes a new instance of PreferencesWindow.
        /// </summary>
        /// <param name="updateManager">Optional UpdateManager instance for checking updates.</param>
        public PreferencesWindow(UpdateManager updateManager = null)
        {
            InitializeComponent();

            this.updateManager = updateManager;

            // Apply MahApps.Metro theme sync with Windows dark/light mode
            ControlzEx.Theming.ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ControlzEx.Theming.ThemeManager.Current.SyncTheme();

            // Load current preferences
            LoadPreferences();

            log.Debug("PreferencesWindow initialized");
        }

        /// <summary>
        /// Loads user preferences and populates UI controls.
        /// </summary>
        private void LoadPreferences()
        {
            try
            {
                preferences = UserPreferences.Load();

                // Auto-Update settings
                AutoCheckUpdatesCheckBox.IsChecked = preferences.AutoCheckForUpdates;
                CheckFrequencyUpDown.Value = preferences.CheckFrequencyHours;

                // Last check info
                if (preferences.LastUpdateCheck == DateTime.MinValue)
                {
                    LastCheckText.Text = "Last check: Never";
                }
                else
                {
                    var timeSince = DateTime.UtcNow - preferences.LastUpdateCheck;
                    if (timeSince.TotalDays >= 1)
                        LastCheckText.Text = $"Last check: {timeSince.TotalDays:F0} days ago";
                    else if (timeSince.TotalHours >= 1)
                        LastCheckText.Text = $"Last check: {timeSince.TotalHours:F0} hours ago";
                    else
                        LastCheckText.Text = $"Last check: {timeSince.TotalMinutes:F0} minutes ago";
                }

                // Skipped version
                if (string.IsNullOrEmpty(preferences.LastSkippedVersion))
                {
                    SkippedVersionText.Text = "Skipped version: None";
                }
                else
                {
                    SkippedVersionText.Text = $"Skipped version: {preferences.LastSkippedVersion}";
                }

                // API Keys
                RawgApiKeyTextBox.Text = preferences.RawgApiKey ?? string.Empty;

                // General settings
                ShowWelcomeTipsCheckBox.IsChecked = !preferences.HasSeenWelcomeTips;

                // Statistics
                FenceCountText.Text = $"Fences created: {preferences.FencesCreatedCount}";

                if (preferences.FirstRunDate == DateTime.MinValue)
                {
                    FirstRunText.Text = "First run: Unknown";
                }
                else
                {
                    FirstRunText.Text = $"First run: {preferences.FirstRunDate:yyyy-MM-dd}";
                }

                // Version info
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                VersionText.Text = $"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

                log.Debug("Preferences loaded successfully");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading preferences: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to load preferences: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles changes to auto-check checkbox.
        /// </summary>
        private void AutoCheckUpdatesCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Enable/disable frequency controls based on auto-check state
            if (CheckFrequencyUpDown != null)
            {
                CheckFrequencyUpDown.IsEnabled = AutoCheckUpdatesCheckBox.IsChecked == true;
            }
        }

        /// <summary>
        /// Handles "Check Now" button click - manually check for updates.
        /// </summary>
        private async void CheckNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (updateManager == null)
            {
                log.Warn("UpdateManager not available for manual check");
                MessageBox.Show(
                    "Update manager is not available.",
                    "Cannot Check for Updates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                CheckNowButton.IsEnabled = false;
                CheckNowButton.Content = "Checking...";

                log.Info("User initiated manual update check from preferences");

                var (result, updateInfo) = await updateManager.CheckForUpdatesAsync();

                // Update last check time immediately
                preferences.LastUpdateCheck = DateTime.UtcNow;
                preferences.Save();

                // Refresh display
                var timeSince = DateTime.UtcNow - preferences.LastUpdateCheck;
                LastCheckText.Text = "Last check: Just now";

                switch (result)
                {
                    case Model.UpdateCheckResult.UpdateAvailable:
                        log.Info($"Manual check found update: {updateInfo.Version}");
                        updateManager.ShowUpdateNotification(updateInfo);
                        break;

                    case Model.UpdateCheckResult.UpToDate:
                        log.Info("Manual check: software is up to date");
                        updateManager.ShowUpToDateMessage();
                        break;

                    case Model.UpdateCheckResult.Error:
                        log.Warn("Manual check failed");
                        updateManager.ShowUpdateCheckError();
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error during manual update check: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to check for updates: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                CheckNowButton.IsEnabled = true;
                CheckNowButton.Content = "Check Now";
            }
        }

        /// <summary>
        /// Handles GitHub button click - opens project repository.
        /// </summary>
        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                log.Info("Opening GitHub repository from preferences");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/marllonsimoes/NoFences-main",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open GitHub page: {ex.Message}", ex);
                MessageBox.Show(
                    "Could not open browser.\n\nPlease visit: https://github.com/marllonsimoes/NoFences-main",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles Save button click - persists preferences and closes window.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate frequency value
                if (CheckFrequencyUpDown.Value < 1 || CheckFrequencyUpDown.Value > 168)
                {
                    MessageBox.Show(
                        "Check frequency must be between 1 and 168 hours (7 days).",
                        "Invalid Frequency",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Update preferences
                preferences.AutoCheckForUpdates = AutoCheckUpdatesCheckBox.IsChecked == true;
                preferences.CheckFrequencyHours = (int)CheckFrequencyUpDown.Value;

                // Update API Keys
                preferences.RawgApiKey = string.IsNullOrWhiteSpace(RawgApiKeyTextBox.Text)
                    ? null
                    : RawgApiKeyTextBox.Text.Trim();

                // Update welcome tips setting (inverted logic - checkbox is "Show tips")
                if (ShowWelcomeTipsCheckBox.IsChecked == false)
                {
                    // User unchecked "Show tips" - mark as seen
                    preferences.HasSeenWelcomeTips = true;
                }
                else
                {
                    // User checked "Show tips" - reset to show again
                    preferences.HasSeenWelcomeTips = false;
                }

                // Save to XML
                preferences.Save();

                log.Info($"Preferences saved - AutoCheck: {preferences.AutoCheckForUpdates}, Frequency: {preferences.CheckFrequencyHours}h");

                MessageBox.Show(
                    "Preferences saved successfully.\n\nChanges will take effect immediately.",
                    "Preferences Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                log.Error($"Error saving preferences: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to save preferences: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles Cancel button click - closes window without saving.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            log.Debug("User cancelled preferences window");
            this.DialogResult = false;
            this.Close();
        }
    }
}
