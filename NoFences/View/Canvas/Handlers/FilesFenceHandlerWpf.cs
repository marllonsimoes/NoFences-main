using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;

namespace NoFences.View.Canvas.Handlers
{
    /// <summary>
    /// WPF-based handler for displaying files and folders in a fence.
    /// This is part of the NEW canvas-based architecture.
    ///
    /// For the original WinForms version, see View/Fences/Handlers/FilesFenceHandler.cs
    /// </summary>
    public class FilesFenceHandlerWpf : IFenceHandlerWpf
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FilesFenceHandlerWpf));

        private FenceInfo fenceInfo;
        private ItemsControl itemsControl;
        private ScrollViewer scrollViewer;
        private ObservableCollection<FileItemViewModel> items;
        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();

        // Icon cache - stores extracted BitmapSource objects for reuse across refreshes
        // Session 11: Changed from Dictionary<string, string> (path -> iconPath) to Dictionary<string, BitmapSource> (path -> extracted icon)
        private Dictionary<string, BitmapSource> extractedIconCache = new Dictionary<string, BitmapSource>();

        // Event raised when content changes (for auto-height)
        public event EventHandler ContentChanged;

        // Cached InstalledSoftware items - Session 11: Changed from List<string> to List<InstalledSoftware>
        private List<InstalledSoftware> installedItems = new List<InstalledSoftware>();

        public void Initialize(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
            this.items = new ObservableCollection<FileItemViewModel>();

            // Pre-load items during initialization
            installedItems = GetInstalledItems();

            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;

            log.Debug($"Initialized for fence '{fenceInfo.Name}', loaded {installedItems.Count} items");
        }

        public UIElement CreateContentElement(int titleHeight, FenceThemeDefinition theme)
        {
            log.Debug($"Creating content element with titleHeight={titleHeight}");

            // Create scroll viewer with theme background
            var contentBg = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                theme.ContentBackgroundColor.A,
                theme.ContentBackgroundColor.R,
                theme.ContentBackgroundColor.G,
                theme.ContentBackgroundColor.B));

            scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = contentBg,
                // Session 11: Increased top padding from 12 to 25 to make room for badges on first row
                Padding = new System.Windows.Thickness(5, 25, 5, 5) // Left, Top, Right, Bottom
            };

            // Create items control with wrap panel
            itemsControl = new ItemsControl
            {
                ItemsSource = items,
                Background = Brushes.Transparent
            };

            // Set up wrap panel
            var panelFactory = new FrameworkElementFactory(typeof(WrapPanel));
            panelFactory.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
            itemsControl.ItemsPanel = new ItemsPanelTemplate(panelFactory);

            // Set up item template with theme colors using FileItemTemplateBuilder
            itemsControl.ItemTemplate = FileItemTemplateBuilder.Create(theme, OpenFile);

            scrollViewer.Content = itemsControl;

            // Load files
            Refresh();
            log.Debug($"Content element created, items count: {items.Count}");

            return scrollViewer;
        }

        public void Refresh()
        {
            log.Debug($"Refreshing content for fence '{fenceInfo.Name}'");
            items.Clear();

            // Reload items if cache is empty
            if (installedItems.Count == 0)
            {
                installedItems = GetInstalledItems();
            }

            log.Debug($"Found {installedItems.Count} installed items to display");

            foreach (var software in installedItems)
            {
                // Get the path to use for caching (ExecutablePath or InstallLocation)
                string cachePath = software.ExecutablePath ?? software.InstallLocation;
                if (string.IsNullOrEmpty(cachePath))
                {
                    log.Warn($"Skipping item '{software.Name}' - no valid path");
                    continue;
                }

                // Check icon cache first - massive performance improvement!
                BitmapSource icon;
                if (!extractedIconCache.TryGetValue(cachePath, out icon))
                {
                    // Icon not cached - extract and cache it
                    icon = ExtractIconFromSoftware(software);
                    if (icon != null)
                    {
                        extractedIconCache[cachePath] = icon;
                        log.Debug($"Cached icon for '{software.Name}' at path '{cachePath}'");
                    }
                }

                // Create view model with full metadata
                items.Add(FileItemViewModel.FromInstalledSoftware(software, icon));
            }

            log.Debug($"Refresh complete, {items.Count} items added ({extractedIconCache.Count} icons cached)");

            // Notify content changed for auto-height adjustment
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets installed items (software or files) based on fence configuration.
        /// Session 11: Changed from GetFiles() returning List<string> to GetInstalledItems() returning List<InstalledSoftware>.
        /// </summary>
        private List<InstalledSoftware> GetInstalledItems()
        {
            var itemList = new List<InstalledSoftware>();

            // Check if new smart filter is configured
            if (fenceInfo.Filter != null)
            {
                var filterResult = FileFenceFilter.ApplyFilter(fenceInfo.Filter, fenceInfo.Path, fenceInfo.Items);

                // Return all items (software + files) - full metadata preserved!
                itemList.AddRange(filterResult.AllItems);

                log.Debug($"Smart filter returned {filterResult.SoftwareItems.Count} software items + {filterResult.FileItems.Count} file items");
                return itemList;
            }

            // Legacy filtering with old Filters list - convert paths to InstalledSoftware
            if (!string.IsNullOrEmpty(fenceInfo.Path))
            {
                if (!Directory.Exists(fenceInfo.Path))
                {
                    log.Warn($"Legacy filter: Path does not exist: {fenceInfo.Path}");
                    return itemList;
                }

                var directoryFilesList = Directory.GetFileSystemEntries(fenceInfo.Path).ToList();
                if (fenceInfo.Filters?.Count > 0)
                {
                    foreach (string pattern in fenceInfo.Filters)
                    {
                        var matchedPaths = directoryFilesList.Where(p => FileFenceFilter.MatchesPattern(p, pattern));
                        foreach (var path in matchedPaths)
                        {
                            itemList.Add(InstalledSoftware.FromPath(path));
                        }
                    }
                }
                else
                {
                    foreach (var path in directoryFilesList)
                    {
                        itemList.Add(InstalledSoftware.FromPath(path));
                    }
                }

                log.Debug($"Legacy filter returned {itemList.Count} items from path '{fenceInfo.Path}'");
            }
            else if (fenceInfo.Items?.Count > 0)
            {
                foreach (var item in fenceInfo.Items.Where(item => Directory.Exists(item) || File.Exists(item)))
                {
                    itemList.Add(InstalledSoftware.FromPath(item));
                }

                log.Debug($"Manual items returned {itemList.Count} items");
            }

            return itemList;
        }

        /// <summary>
        /// Extracts icon from InstalledSoftware, with smart fallback logic.
        /// Session 11: Replaced ExtractIcon(FenceEntry) to work with InstalledSoftware objects.
        /// </summary>
        private BitmapSource ExtractIconFromSoftware(InstalledSoftware software)
        {
            try
            {
                Icon icon = null;

                // Priority 1: Use cached icon if available
                if (software.CachedIcon != null)
                {
                    log.Debug($"Using cached GDI+ icon for '{software.Name}'");
                    icon = software.CachedIcon;
                }
                // Priority 2: Use pre-extracted IconPath (Steam, GOG, etc.)
                else if (!string.IsNullOrEmpty(software.IconPath) && File.Exists(software.IconPath))
                {
                    try
                    {
                        icon = Icon.ExtractAssociatedIcon(software.IconPath);
                        software.CachedIcon = icon; // Cache for next time
                        log.Debug($"Extracted icon from IconPath '{software.IconPath}' for '{software.Name}'");
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"Failed to extract icon from IconPath '{software.IconPath}': {ex.Message}");
                    }
                }

                // Priority 3: Extract from ExecutablePath
                if (icon == null && !string.IsNullOrEmpty(software.ExecutablePath) && File.Exists(software.ExecutablePath))
                {
                    if (thumbnailProvider.IsSupported(software.ExecutablePath))
                    {
                        icon = thumbnailProvider.GenerateThumbnail(software.ExecutablePath);
                    }
                    else
                    {
                        icon = Icon.ExtractAssociatedIcon(software.ExecutablePath);
                    }

                    software.CachedIcon = icon; // Cache for next time
                    log.Debug($"Extracted icon from ExecutablePath '{software.ExecutablePath}' for '{software.Name}'");
                }

                // Priority 4: Use folder icon for InstallLocation
                if (icon == null && !string.IsNullOrEmpty(software.InstallLocation) && Directory.Exists(software.InstallLocation))
                {
                    icon = IconUtil.FolderLarge;
                    log.Debug($"Using folder icon for '{software.Name}'");
                }

                // Fallback: Default document icon
                if (icon == null)
                {
                    icon = IconUtil.DocumentFile;
                    log.Debug($"Using default document icon for '{software.Name}'");
                }

                // Convert GDI+ Icon to WPF BitmapSource
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Apply image preprocessing to replace pure black pixels with near-black
                // This prevents transparency issues when icons contain pure black (RGB 0,0,0)
                return ImagePreprocessor.PreprocessImage(bitmapSource);
            }
            catch (Exception ex)
            {
                // Return default icon on error
                log.Error($"Error extracting icon for '{software.Name}': {ex.Message}", ex);
                try
                {
                    var defaultIcon = IconUtil.DocumentFile;
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        defaultIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    return ImagePreprocessor.PreprocessImage(bitmapSource);
                }
                catch
                {
                    return null;
                }
            }
        }

        private void ThumbnailProvider_IconThumbnailLoaded(object sender, EventArgs e)
        {
            // Refresh items when thumbnails are loaded
            Application.Current?.Dispatcher.BeginInvoke(new Action(() => Refresh()));
        }

        private void OpenFile(string path)
        {
            try
            {
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to open file: {ex.Message}", ex);
            }
        }

        public void Cleanup()
        {
            thumbnailProvider.IconThumbnailLoaded -= ThumbnailProvider_IconThumbnailLoaded;
            items?.Clear();
        }

        public bool HasContent()
        {
            // Files fence has content if there are items to display
            return installedItems != null && installedItems.Count > 0;
        }
    }

    /// <summary>
    /// View model for file/folder items displayed in the fence.
    /// Enhanced in Session 11 to include full metadata from InstalledSoftware.
    /// </summary>
    public class FileItemViewModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public BitmapSource Icon { get; set; }

        // NEW: Metadata fields (Session 11)
        public string Publisher { get; set; }
        public string Version { get; set; }
        public DateTime? InstallDate { get; set; }
        public string Source { get; set; }
        public SoftwareCategory Category { get; set; }

        // UI helper properties
        public bool HasVersion => !string.IsNullOrEmpty(Version);
        public bool HasPublisher => !string.IsNullOrEmpty(Publisher);

        public bool IsRecentlyInstalled
        {
            get
            {
                if (!InstallDate.HasValue) return false;
                return (DateTime.Now - InstallDate.Value).TotalDays < 7;
            }
        }

        public bool IsSteam => Source?.Contains("Steam") == true;
        public bool IsEpic => Source?.Contains("Epic") == true;
        public bool IsGOG => Source?.Contains("GOG") == true;
        public bool IsLocal => Source == "Local";

        public string Tooltip
        {
            get
            {
                var lines = new List<string> { Name };

                if (HasPublisher)
                    lines.Add($"Publisher: {Publisher}");

                if (HasVersion)
                    lines.Add($"Version: {Version}");

                if (InstallDate.HasValue)
                    lines.Add($"Installed: {InstallDate.Value:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(Source))
                    lines.Add($"Source: {Source}");

                lines.Add($"Path: {Path}");

                return string.Join("\n", lines);
            }
        }

        /// <summary>
        /// Factory method to create FileItemViewModel from InstalledSoftware.
        /// Session 11: Preserves all metadata from database.
        /// </summary>
        public static FileItemViewModel FromInstalledSoftware(InstalledSoftware software, BitmapSource icon)
        {
            return new FileItemViewModel
            {
                Name = software.Name,
                Path = software.ExecutablePath ?? software.InstallLocation,
                Icon = icon,
                Publisher = software.Publisher,
                Version = software.Version,
                InstallDate = software.InstallDate,
                Source = software.Source,
                Category = software.Category
            };
        }
    }
}
