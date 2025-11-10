using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private Dictionary<string, string> iconPathCache = new Dictionary<string, string>();

        // Event raised when content changes (for auto-height)
        public event EventHandler ContentChanged;

        // TODO cache the files
        private List<string> files = new List<string>();

        public void Initialize(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
            this.items = new ObservableCollection<FileItemViewModel>();
            var files = GetFiles();

            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;

            log.Debug($"Initialized for fence '{fenceInfo.Name}', Path='{fenceInfo.Path}'");
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
                Padding = new System.Windows.Thickness(5, 12, 5, 5) // Left, Top, Right, Bottom - extra top space from title
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
            iconPathCache.Clear();

            if (files.Count == 0)
            {
                files = GetFiles();
            }

            log.Debug($"Found {files.Count} files to display");

            foreach (var filePath in files)
            {
                // Check if we have a cached icon path for this file
                string iconPath = iconPathCache.ContainsKey(filePath) ? iconPathCache[filePath] : null;

                var entry = FenceEntry.FromPath(filePath, iconPath);
                if (entry != null)
                {
                    var icon = ExtractIcon(entry);
                    items.Add(new FileItemViewModel
                    {
                        Name = entry.Name,
                        Path = entry.Path,
                        Icon = icon
                    });
                }
            }

            log.Debug($"Refresh complete, {items.Count} items added");

            // Notify content changed for auto-height adjustment
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private List<string> GetFiles()
        {
            var fileList = new List<string>();

            // Check if new smart filter is configured
            if (fenceInfo.Filter != null)
            {
                var filterResult = FileFenceFilter.ApplyFilter(fenceInfo.Filter, fenceInfo.Path, fenceInfo.Items);

                // Merge icon cache entries
                foreach (var kvp in filterResult.IconCache)
                {
                    iconPathCache[kvp.Key] = kvp.Value;
                }

                return filterResult.FilePaths;
            }

            // Legacy filtering with old Filters list
            if (!string.IsNullOrEmpty(fenceInfo.Path))
            {
                if (!Directory.Exists(fenceInfo.Path))
                    return fileList;

                var directoryFilesList = Directory.GetFileSystemEntries(fenceInfo.Path).ToList();
                if (fenceInfo.Filters?.Count > 0)
                {
                    foreach (string pattern in fenceInfo.Filters)
                    {
                        fileList.AddRange(directoryFilesList.Where(p => FileFenceFilter.MatchesPattern(p, pattern)));
                    }
                }
                else
                {
                    fileList.AddRange(directoryFilesList);
                }
            }
            else if (fenceInfo.Items?.Count > 0)
            {
                fileList.AddRange(fenceInfo.Items.Where(item => Directory.Exists(item) || File.Exists(item)));
            }

            return fileList;
        }

        private BitmapSource ExtractIcon(FenceEntry entry)
        {
            try
            {
                var icon = entry.ExtractIcon(thumbnailProvider);
                if (icon == null)
                {
                    // Use default icon if extraction returned null
                    icon = IconUtil.DocumentFile;
                }

                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Apply image preprocessing to replace pure black pixels with near-black
                // This prevents transparency issues when icons contain pure black (RGB 0,0,0)
                // which would become transparent due to the canvas TransparencyKey
                return ImagePreprocessor.PreprocessImage(bitmapSource);
            }
            catch (Exception ex)
            {
                // Return default icon on error
                log.Error($"Error extracting icon for '{entry.Path}': {ex.Message}", ex);
                try
                {
                    var defaultIcon = IconUtil.DocumentFile;
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        defaultIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    // Also preprocess the default icon
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
            //var files = GetFiles();
            return files != null && files.Count > 0;
        }
    }

    /// <summary>
    /// View model for file/folder items displayed in the fence.
    /// </summary>
    public class FileItemViewModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public BitmapSource Icon { get; set; }
    }
}
