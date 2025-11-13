using NoFences.Core.Util;
using NoFences.Model;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using log4net;
using Microsoft.Win32;
using NoFences.Model.Canvas;
using NoFences.View;

namespace NoFences.Services.Managers
{
    /// <summary>
    /// Manages the system tray icon using WinForms NotifyIcon.
    /// Based on Lively Wallpaper's proven implementation.
    /// </summary>
    internal class TrayIconManager : IApplicationService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TrayIconManager));
        private readonly NotifyIcon notifyIcon = new NotifyIcon();
        private ToolStripMenuItem toggleStartUpMenuItem;
        private ContextMenuStrip contextMenu;
        private readonly UpdateManager updateManager;

        public TrayIconManager()
        {
            // Initialize UpdateManager with default GitHub API URL and tray icon reference
            // Note: notifyIcon will be null here but can be set later via SetNotifyIcon()
            updateManager = new UpdateManager(null, null);
        }

        /// <summary>
        /// Sets the NotifyIcon reference after initialization (for balloon notifications).
        /// </summary>
        private void SetUpdateManagerTrayIcon()
        {
            // Use reflection to set private field (workaround for initialization order)
            var trayIconField = typeof(UpdateManager).GetField("trayIcon",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            trayIconField?.SetValue(updateManager, notifyIcon);
            log.Debug("NotifyIcon reference passed to UpdateManager");
        }

        public void Start()
        {
            try
            {
                log.Debug("=== TrayIconManager.Start() - BEGIN ===");

                // Critical WPF workaround from Lively
                // NotifyIcon Issue: "The root Visual of a VisualTarget cannot have a parent.."
                // Ref: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
                log.Debug("Applying WPF tooltip workaround...");
                var tt = new System.Windows.Controls.ToolTip();
                tt.IsOpen = true;
                tt.IsOpen = false;
                log.Debug("WPF tooltip workaround applied");

                // Initialize NotifyIcon properties in the correct order (like Lively)
                log.Debug("Setting up NotifyIcon...");

                // 1. Load icon from file path (simpler and more reliable)
                log.Debug("Loading icon...");
                try
                {
                    string iconPath = Path.Combine(
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        "fibonacci.ico");
                    log.Debug($"Icon path: {iconPath}");

                    if (File.Exists(iconPath))
                    {
                        notifyIcon.Icon = new Icon(iconPath, new Size(16, 16));
                        log.Debug($"Icon loaded from file: {iconPath}");
                    }
                    else
                    {
                        log.Warn($"Icon file not found at: {iconPath}, using default");
                        notifyIcon.Icon = SystemIcons.Application;
                        log.Info("Using default application icon");
                    }
                }
                catch (Exception iconEx)
                {
                    log.Error($"Icon loading failed: {iconEx.Message}", iconEx);
                    notifyIcon.Icon = SystemIcons.Application;
                    log.Info("Using default application icon");
                }

                // 2. Set tooltip text
                notifyIcon.Text = "NoFences - Desktop Organization";

                // 3. Create context menu
                log.Debug("Creating context menu...");
                contextMenu = CreateContextMenu();
                notifyIcon.ContextMenuStrip = contextMenu;
                log.Debug($"Context menu created with {contextMenu.Items.Count} items");

                // 4. Listen for Windows theme changes
                Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
                log.Debug("Subscribed to system theme changes");

                // 5. Make visible (CRITICAL - must be last)
                notifyIcon.Visible = true;
                log.Debug("NotifyIcon visibility set to true");

                // 6. Pass NotifyIcon to UpdateManager for balloon notifications
                SetUpdateManagerTrayIcon();

                // 7. Start UpdateManager service (starts auto-check timer)
                updateManager.Start();

                log.Debug("=== TrayIconManager.Start() - END (SUCCESS) ===");
            }
            catch (Exception ex)
            {
                log.Error($"=== TrayIconManager.Start() - FAILED: {ex.Message} ===", ex);
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                log.Debug("=== TrayIconManager.Stop() - BEGIN ===");

                // Unsubscribe from theme changes
                Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
                log.Debug("Unsubscribed from system theme changes");

                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Icon?.Dispose();
                    notifyIcon.Dispose();
                    log.Debug("NotifyIcon disposed");
                }

                contextMenu?.Dispose();

                // Stop UpdateManager service (stops auto-check timer)
                updateManager?.Stop();

                log.Debug("=== TrayIconManager.Stop() - END ===");
            }
            catch (Exception ex)
            {
                log.Error($"Error stopping TrayIconManager: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles Windows theme changes and updates context menu accordingly
        /// </summary>
        private void OnSystemThemeChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            try
            {
                // Only respond to theme changes
                if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
                {
                    log.Debug("System theme changed, updating context menu...");

                    // Update theme on UI thread
                    if (contextMenu?.InvokeRequired == true)
                    {
                        contextMenu.Invoke(new Action(() => ApplyTheme(contextMenu)));
                    }
                    else
                    {
                        ApplyTheme(contextMenu);
                    }

                    log.Debug("Context menu theme updated");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error handling theme change: {ex.Message}", ex);
            }
        }

        private ContextMenuStrip CreateContextMenu()
        {
            log.Debug("CreateContextMenu - Creating WinForms context menu...");
            var contextMenu = new ContextMenuStrip
            {
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Apply dark/light theme
            ApplyTheme(contextMenu);

            // Start on login (with checkmark)
            toggleStartUpMenuItem = new ToolStripMenuItem
            {
                Text = "Start on login",
                CheckOnClick = true,
                Checked = StartupManager.IsAutoStartEnabled()
            };
            toggleStartUpMenuItem.Click += (s, e) => ToggleAutoStart();
            contextMenu.Items.Add(toggleStartUpMenuItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // New Fence
            var newFenceItem = new ToolStripMenuItem
            {
                Text = "New Fence"
            };
            newFenceItem.Click += (s, e) =>
            {
                CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default
                    .GetService<FenceManager>()
                    .CreateFence("New Fence");
            };
            contextMenu.Items.Add(newFenceItem);

            // Open local storage
            var openStorageItem = new ToolStripMenuItem
            {
                Text = "Open local storage"
            };
            openStorageItem.Click += (s, e) => OpenLocalStorage();
            contextMenu.Items.Add(openStorageItem);

            // Service status window
            var serviceStatusItem = new ToolStripMenuItem
            {
                Text = "Status"
            };
            serviceStatusItem.Click += (s, e) => OpenServiceStatusWindow();
            contextMenu.Items.Add(serviceStatusItem);

            // Refresh Software Database
            var refreshDatabaseItem = new ToolStripMenuItem
            {
                Text = "Refresh Software Database..."
            };
            refreshDatabaseItem.Click += async (s, e) => await RefreshSoftwareDatabase();
            contextMenu.Items.Add(refreshDatabaseItem);

            // Enrich Software Metadata (Session 11: Metadata enrichment integration)
            var enrichMetadataItem = new ToolStripMenuItem
            {
                Text = "Enrich Software Metadata..."
            };
            enrichMetadataItem.Click += async (s, e) => await EnrichSoftwareMetadata();
            contextMenu.Items.Add(enrichMetadataItem);

            // Log viewer with submenu for log level
            var logViewerItem = new ToolStripMenuItem
            {
                Text = "Logs"
            };

            // View Logs submenu item
            var viewLogsItem = new ToolStripMenuItem
            {
                Text = "View Logs..."
            };
            viewLogsItem.Click += (s, e) => OpenLogViewer();
            logViewerItem.DropDownItems.Add(viewLogsItem);

            logViewerItem.DropDownItems.Add(new ToolStripSeparator());

            // Log Level submenu
            var currentLevel = GetCurrentLogLevel();

            var debugLevelItem = new ToolStripMenuItem
            {
                Text = "DEBUG",
                CheckOnClick = false,
                Checked = currentLevel == "DEBUG"
            };
            debugLevelItem.Click += (s, e) => SetLogLevel("DEBUG");
            logViewerItem.DropDownItems.Add(debugLevelItem);

            var infoLevelItem = new ToolStripMenuItem
            {
                Text = "INFO",
                CheckOnClick = false,
                Checked = currentLevel == "INFO"
            };
            infoLevelItem.Click += (s, e) => SetLogLevel("INFO");
            logViewerItem.DropDownItems.Add(infoLevelItem);

            var warnLevelItem = new ToolStripMenuItem
            {
                Text = "WARN",
                CheckOnClick = false,
                Checked = currentLevel == "WARN"
            };
            warnLevelItem.Click += (s, e) => SetLogLevel("WARN");
            logViewerItem.DropDownItems.Add(warnLevelItem);

            var errorLevelItem = new ToolStripMenuItem
            {
                Text = "ERROR",
                CheckOnClick = false,
                Checked = currentLevel == "ERROR"
            };
            errorLevelItem.Click += (s, e) => SetLogLevel("ERROR");
            logViewerItem.DropDownItems.Add(errorLevelItem);

            contextMenu.Items.Add(logViewerItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Settings/Preferences
            var settingsItem = new ToolStripMenuItem
            {
                Text = "Settings..."
            };
            settingsItem.Click += (s, e) => OpenPreferences();
            contextMenu.Items.Add(settingsItem);

            // Check for Updates
            var checkUpdatesItem = new ToolStripMenuItem
            {
                Text = "Check for Updates..."
            };
            checkUpdatesItem.Click += async (s, e) => await CheckForUpdates();
            contextMenu.Items.Add(checkUpdatesItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem
            {
                Text = "Exit"
            };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            log.Debug($"Context menu created with {contextMenu.Items.Count} items");
            return contextMenu;
        }

        private void ToggleAutoStart()
        {
            bool isEnabled = StartupManager.IsAutoStartEnabled();
            StartupManager.ToggleAutoStart(!isEnabled);
            toggleStartUpMenuItem.Checked = !isEnabled;
            log.Debug($"Auto-start toggled: {!isEnabled}");
        }

        private void OpenLocalStorage()
        {
            try
            {
                string basePath = Path.Combine(AppEnvUtil.GetAppEnvironmentPath(), "Fences");
                var psi = new ProcessStartInfo() { FileName = basePath, UseShellExecute = true };
                Process.Start(psi);
                log.Debug($"Opened local storage: {basePath}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open local storage: {ex.Message}", ex);
            }
        }

        // TODO: Review how to call the ServiceStatusWindow from here
        private void OpenServiceStatusWindow()
        {
            try
            {
                new ServiceStatusWindow().Show();
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open local storage: {ex.Message}", ex);
            }
        }

        private void OpenLogViewer()
        {
            try
            {
                string logFilePath = GetLogFilePath();

                if (File.Exists(logFilePath))
                {
                    // Open log file with default text editor
                    var psi = new ProcessStartInfo()
                    {
                        FileName = logFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    log.Info($"Opened log file: {logFilePath}");
                }
                else
                {
                    log.Warn($"Log file not found: {logFilePath}");
                    MessageBox.Show(
                        "Log file not found. Logging may not be configured or no logs have been written yet.",
                        "NoFences - Log Viewer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open log viewer: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to open log file: {ex.Message}",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string GetLogFilePath()
        {
            // Get log file path from log4net configuration
            var repository = log4net.LogManager.GetRepository();
            foreach (var appender in repository.GetAppenders())
            {
                if (appender is log4net.Appender.FileAppender fileAppender)
                {
                    return fileAppender.File;
                }
                else if (appender is log4net.Appender.RollingFileAppender rollingAppender)
                {
                    return rollingAppender.File;
                }
            }

            // Fallback to default location if not configured
            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NoFences",
                "Logs",
                "nofences.log");

            return defaultPath;
        }

        private string GetCurrentLogLevel()
        {
            try
            {
                var repository = log4net.LogManager.GetRepository();
                var hierarchy = repository as log4net.Repository.Hierarchy.Hierarchy;
                if (hierarchy != null)
                {
                    var rootLogger = hierarchy.Root;
                    return rootLogger?.Level?.Name ?? "INFO";
                }
                return "INFO";
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to get current log level: {ex.Message}");
                return "INFO";
            }
        }

        private void SetLogLevel(string level)
        {
            try
            {
                var repository = log4net.LogManager.GetRepository();
                var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;

                log4net.Core.Level log4netLevel;
                switch (level)
                {
                    case "DEBUG":
                        log4netLevel = log4net.Core.Level.Debug;
                        break;
                    case "INFO":
                        log4netLevel = log4net.Core.Level.Info;
                        break;
                    case "WARN":
                        log4netLevel = log4net.Core.Level.Warn;
                        break;
                    case "ERROR":
                        log4netLevel = log4net.Core.Level.Error;
                        break;
                    default:
                        log4netLevel = log4net.Core.Level.Info;
                        break;
                }

                hierarchy.Root.Level = log4netLevel;
                hierarchy.RaiseConfigurationChanged(EventArgs.Empty);

                log.Info($"Log level changed to: {level}");

                // Recreate context menu to update checkmarks
                if (contextMenu != null)
                {
                    contextMenu.Dispose();
                    contextMenu = CreateContextMenu();
                    notifyIcon.ContextMenuStrip = contextMenu;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to set log level: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to change log level: {ex.Message}",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks for software updates asynchronously and shows appropriate notification.
        /// </summary>
        private async System.Threading.Tasks.Task CheckForUpdates()
        {
            try
            {
                log.Info("Manual update check triggered from tray menu");

                // Show checking message (optional - can be removed if too intrusive)
                // MessageBox.Show("Checking for updates...", "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var (result, updateInfo) = await updateManager.CheckForUpdatesAsync();

                switch (result)
                {
                    case UpdateCheckResult.UpdateAvailable:
                        log.Info($"Update available: {updateInfo.Version}");
                        updateManager.ShowUpdateNotification(updateInfo);
                        break;

                    case UpdateCheckResult.UpToDate:
                        log.Info("Software is up to date");
                        updateManager.ShowUpToDateMessage();
                        break;

                    case UpdateCheckResult.Error:
                        log.Warn("Update check failed");
                        updateManager.ShowUpdateCheckError();
                        break;

                    case UpdateCheckResult.Skipped:
                        log.Debug("Update check was skipped");
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error during update check: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to check for updates: {ex.Message}",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Opens the preferences/settings window.
        /// </summary>
        private void OpenPreferences()
        {
            try
            {
                log.Info("Opening preferences window from tray menu");

                // Open preferences window on WPF UI thread
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var preferencesWindow = new PreferencesWindow(updateManager);
                    preferencesWindow.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open preferences window: {ex.Message}", ex);
                MessageBox.Show(
                    $"Failed to open settings window: {ex.Message}",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Refreshes the software database by scanning the system for installed software.
        /// Session 11: Priority 1 - Database Population Mechanism
        /// </summary>
        private async System.Threading.Tasks.Task RefreshSoftwareDatabase()
        {
            try
            {
                log.Info("Manual software database refresh triggered from tray menu");

                // Show starting message via balloon notification
                notifyIcon.ShowBalloonTip(
                    3000,
                    "NoFences",
                    "Refreshing software database...",
                    ToolTipIcon.Info);

                // Run refresh on background thread to avoid blocking UI
                int entriesWritten = await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var service = new NoFencesDataLayer.Services.InstalledSoftwareService();
                        return service.RefreshInstalledSoftware();
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error during database refresh: {ex.Message}", ex);
                        throw;
                    }
                });

                if (entriesWritten > 0)
                {
                    log.Info($"Software database refresh complete: {entriesWritten} entries");

                    // Show success message via balloon notification
                    notifyIcon.ShowBalloonTip(
                        5000,
                        "NoFences - Database Refresh Complete",
                        $"Successfully scanned and stored {entriesWritten} software entries.\n\nFilesFences will now use the database for faster queries.",
                        ToolTipIcon.Info);

                    // Also show a message box for immediate feedback
                    MessageBox.Show(
                        $"Software database refreshed successfully!\n\n" +
                        $"Entries: {entriesWritten}\n" +
                        $"Status: Database is now populated and ready for use.\n\n" +
                        $"FilesFences will now query the database for significantly faster performance.",
                        "NoFences - Database Refresh Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    log.Warn("Software database refresh completed but no entries were written");

                    MessageBox.Show(
                        "Database refresh completed, but no software was detected.\n\n" +
                        "This may indicate an issue with the detection system. " +
                        "Check the logs for more details.",
                        "NoFences - Database Refresh",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to refresh software database: {ex.Message}", ex);

                MessageBox.Show(
                    $"Failed to refresh software database:\n\n{ex.Message}\n\n" +
                    $"FilesFences will continue using in-memory detection. " +
                    $"Check the logs for more details.",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Enriches software metadata from external sources (RAWG, Winget, Wikipedia, CNET).
        /// Session 11: Metadata enrichment integration.
        /// </summary>
        private async System.Threading.Tasks.Task EnrichSoftwareMetadata()
        {
            try
            {
                log.Info("Manual metadata enrichment triggered from tray menu");

                // Show starting message via balloon notification
                notifyIcon.ShowBalloonTip(
                    3000,
                    "NoFences",
                    "Enriching software metadata from external sources...",
                    ToolTipIcon.Info);

                // Run enrichment on background thread to avoid blocking UI
                var result = await System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var enrichmentService = new NoFencesDataLayer.Services.Metadata.MetadataEnrichmentService();
                        var softwareService = new NoFencesDataLayer.Services.InstalledSoftwareService();

                        // Get all software from database
                        var allSoftware = softwareService.QueryInstalledSoftware(category: null, source: null);

                        if (allSoftware.Count == 0)
                        {
                            log.Warn("No software found in database for enrichment");
                            return new { Success = false, Message = "Database is empty", EnrichedCount = 0, TotalCount = 0 };
                        }

                        log.Info($"Starting metadata enrichment for {allSoftware.Count} software entries");

                        // Enrich metadata in batch (with database updates)
                        int enrichedCount = await enrichmentService.EnrichBatchAsync(allSoftware, updateDatabase: true);

                        log.Info($"Metadata enrichment complete: {enrichedCount}/{allSoftware.Count} entries enriched");

                        return new { Success = true, Message = "Success", EnrichedCount = enrichedCount, TotalCount = allSoftware.Count };
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error during metadata enrichment: {ex.Message}", ex);
                        throw;
                    }
                });

                if (result.Success)
                {
                    // Show success message via balloon notification
                    notifyIcon.ShowBalloonTip(
                        5000,
                        "NoFences - Metadata Enrichment Complete",
                        $"Enriched {result.EnrichedCount} of {result.TotalCount} software entries with metadata from external sources.",
                        ToolTipIcon.Info);

                    // Also show a message box for immediate feedback
                    var providerStats = new NoFencesDataLayer.Services.Metadata.MetadataEnrichmentService().GetProviderStatistics();

                    MessageBox.Show(
                        $"Metadata enrichment completed successfully!\n\n" +
                        $"Enriched: {result.EnrichedCount} / {result.TotalCount} entries\n" +
                        $"Success Rate: {(result.EnrichedCount * 100.0 / result.TotalCount):F1}%\n\n" +
                        $"Providers Available:\n" +
                        $"  • Game Providers: {providerStats.GameProvidersAvailable}/{providerStats.GameProvidersTotal}\n" +
                        $"  • Software Providers: {providerStats.SoftwareProvidersAvailable}/{providerStats.SoftwareProvidersTotal}\n\n" +
                        $"Enhanced data now includes:\n" +
                        $"  • Publisher information\n" +
                        $"  • Descriptions\n" +
                        $"  • Genres and categories\n" +
                        $"  • Release dates\n" +
                        $"  • Ratings and reviews",
                        "NoFences - Metadata Enrichment Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    log.Warn($"Metadata enrichment completed but database was empty: {result.Message}");

                    MessageBox.Show(
                        "Cannot enrich metadata: Software database is empty.\n\n" +
                        "Please use 'Refresh Software Database' first to populate the database, " +
                        "then try metadata enrichment again.",
                        "NoFences - Metadata Enrichment",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to enrich software metadata: {ex.Message}", ex);

                MessageBox.Show(
                    $"Failed to enrich software metadata:\n\n{ex.Message}\n\n" +
                    $"Possible causes:\n" +
                    $"  • Internet connection required for external APIs\n" +
                    $"  • API keys may need configuration (RAWG)\n" +
                    $"  • Rate limits may be in effect\n\n" +
                    $"Check the logs for more details.",
                    "NoFences - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExitApplication()
        {
            log.Info("Exit requested from tray icon");
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
                Application.Exit();
            });
        }

        /// <summary>
        /// Detects if Windows is in dark mode by checking registry
        /// </summary>
        private bool IsWindowsDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value is int i && i == 0; // 0 = dark mode, 1 = light mode
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to detect Windows theme: {ex.Message}");
                return false; // Default to light mode
            }
        }

        /// <summary>
        /// Applies dark or light theme to the context menu
        /// </summary>
        private void ApplyTheme(ContextMenuStrip menu)
        {
            bool isDarkMode = IsWindowsDarkMode();
            log.Debug($"Applying theme: {(isDarkMode ? "Dark" : "Light")}");

            if (isDarkMode)
            {
                menu.Renderer = new DarkModeRenderer();
            }
            else
            {
                menu.Renderer = new ToolStripProfessionalRenderer();
            }
        }
    }

    /// <summary>
    /// Custom dark mode renderer for ContextMenuStrip.
    /// Based on Windows 11 dark theme colors.
    /// </summary>
    internal class DarkModeRenderer : ToolStripProfessionalRenderer
    {
        public DarkModeRenderer() : base(new DarkModeColorTable())
        {
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            // Draw arrow in white for dark mode
            e.ArrowColor = Color.White;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Text color based on state
            if (e.Item.Selected || e.Item.Pressed)
            {
                e.TextColor = Color.White;
            }
            else if (!e.Item.Enabled)
            {
                e.TextColor = Color.Gray;
            }
            else
            {
                e.TextColor = Color.White;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                // Hover background - Windows 11 style
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 45)), e.Item.ContentRectangle);
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            // Dark separator
            var rect = new Rectangle(e.Item.ContentRectangle.Left, e.Item.ContentRectangle.Height / 2,
                                     e.Item.ContentRectangle.Width, 1);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), rect);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // No image margin coloring in dark mode
        }
    }

    /// <summary>
    /// Color table for dark mode context menu.
    /// Uses Windows 11 dark theme colors.
    /// </summary>
    internal class DarkModeColorTable : ProfessionalColorTable
    {
        // Windows 11 dark theme colors
        private static readonly Color DarkBackground = Color.FromArgb(32, 32, 32);      // Main background
        private static readonly Color DarkItemSelected = Color.FromArgb(45, 45, 45);    // Hover/selected
        private static readonly Color DarkBorder = Color.FromArgb(60, 60, 60);          // Borders
        private static readonly Color DarkText = Color.White;                           // Text

        public override Color MenuStripGradientBegin => DarkBackground;
        public override Color MenuStripGradientEnd => DarkBackground;
        public override Color MenuItemSelected => DarkItemSelected;
        public override Color MenuItemSelectedGradientBegin => DarkItemSelected;
        public override Color MenuItemSelectedGradientEnd => DarkItemSelected;
        public override Color MenuItemBorder => DarkBorder;
        public override Color MenuBorder => DarkBorder;
        public override Color MenuItemPressedGradientBegin => DarkItemSelected;
        public override Color MenuItemPressedGradientEnd => DarkItemSelected;
        public override Color ImageMarginGradientBegin => DarkBackground;
        public override Color ImageMarginGradientMiddle => DarkBackground;
        public override Color ImageMarginGradientEnd => DarkBackground;
        public override Color ToolStripDropDownBackground => DarkBackground;
        public override Color SeparatorDark => DarkBorder;
        public override Color SeparatorLight => DarkBorder;
    }
}
